using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lumen.BusinessLogic.Abstractions;
using Lumen.BusinessLogic.Services;
using Lumen.DataAccess;
using Lumen.DataAccess.Entities;
using Lumen.DataAccess.Repositories;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.API.IntegrationTests;

public sealed class CourseUpdateIntegrationTests
{
    [Fact]
    public async Task Draft_PersistsAllFieldsAndModuleAddModifyRemoveAndSwaps()
    {
        using var factory = new LumenApiFactory();
        using var teacher = await CourseTestClient.LoginAsync(factory);
        var course = await CourseTestClient.DraftAsync(teacher);
        var first = CourseTestClient.Module(course.Modules[0]);
        var third = CourseTestClient.Module(course.Modules[2]) with { Name = "Modificado", Content = "Contenido nuevo", Resources = ["archivo.pdf"] };
        var update = CourseTestClient.Update(course) with { Title = "Actualizado", Description = "Otra descripción",
            Category = "Otra categoría", Level = "Avanzado", Modality = "Presencial", MaxCapacity = 15,
            LearningObjectives = ["Nuevo objetivo"], SuggestedPrerequisites = [],
            Modules = [third, first, new(null, "Agregado", "Desc", 25, "", ["guia.pdf"])] };
        var result = await CourseTestClient.PutAsync(teacher, course.Id, update);
        Assert.Equal("Borrador", result.Status);
        Assert.Equal(115, result.TotalDurationMinutes);
        Assert.Equal(["Modificado", "Primero", "Agregado"], result.Modules.Select(x => x.Name));
        Assert.DoesNotContain(result.Modules, x => x.Id == course.Modules[1].Id);
        var added = result.Modules[2];
        Assert.NotEqual(Guid.Empty, added.Id);

        // All retained IDs swap positions in the same existing revision.
        result = await CourseTestClient.PutAsync(teacher, course.Id, CourseTestClient.Update(result) with
            { Modules = [CourseTestClient.Module(added), first, third] });
        var persisted = await CourseTestClient.GetAsync(teacher, course.Id);
        Assert.Equal([added.Id, first.Id!.Value, third.Id!.Value], persisted.Modules.Select(x => x.Id));
        Assert.Equal([1, 2, 3], persisted.Modules.Select(x => x.Order));
        Assert.Equal("Actualizado", persisted.Title);
        Assert.Equal("Otra descripción", persisted.Description);
        Assert.Equal("Otra categoría", persisted.Category);
        Assert.Equal("Avanzado", persisted.Level);
        Assert.Equal("Presencial", persisted.Modality);
        Assert.Equal(15, persisted.MaxCapacity);
        Assert.Equal(["Nuevo objetivo"], persisted.LearningObjectives);
        Assert.Empty(persisted.SuggestedPrerequisites);
        Assert.Equal(115, persisted.TotalDurationMinutes);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        Assert.Equal(3, await db.CourseModules.CountAsync(x => x.Revision.CourseId == course.Id));
        Assert.False(await db.CourseReviews.AnyAsync(x => x.CourseRevision.CourseId == course.Id));
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Theory]
    [InlineData("alumno@lumen.local", HttpStatusCode.Forbidden)]
    [InlineData("admin@lumen.local", HttpStatusCode.OK)]
    [InlineData("profesor@lumen.local", HttpStatusCode.OK)]
    [InlineData("", HttpStatusCode.Unauthorized)]
    public async Task Update_EnforcesRoles(string email, HttpStatusCode status)
    {
        using var factory = new LumenApiFactory();
        using var teacher = await CourseTestClient.LoginAsync(factory);
        var course = await CourseTestClient.DraftAsync(teacher);
        using var client = email == "" ? factory.CreateClient() : await CourseTestClient.LoginAsync(factory, email);
        var result = await client.PutAsJsonAsync($"/api/courses/{course.Id}", CourseTestClient.Update(course), CourseTestClient.Json);
        Assert.Equal(status, result.StatusCode);
        var submit = await client.PostAsync($"/api/courses/{course.Id}/submit-review", null);
        Assert.Equal(status, submit.StatusCode);
    }

    [Fact]
    public async Task Update_RejectsForeignTeacherAndMissingCourse()
    {
        using var factory = new LumenApiFactory();
        using var teacher = await CourseTestClient.LoginAsync(factory);
        using var admin = await CourseTestClient.LoginAsync(factory, "admin@lumen.local");
        var course = await CourseTestClient.DraftAsync(admin);
        var before = JsonSerializer.Serialize(await CourseTestClient.GetAsync(admin, course.Id));
        var response = await teacher.PutAsJsonAsync($"/api/courses/{course.Id}", CourseTestClient.Update(course), CourseTestClient.Json);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await teacher.PostAsync($"/api/courses/{course.Id}/submit-review", null)).StatusCode);
        Assert.Equal(before, JsonSerializer.Serialize(await CourseTestClient.GetAsync(admin, course.Id)));
        Assert.Equal(HttpStatusCode.NotFound, (await teacher.PutAsJsonAsync($"/api/courses/{Guid.NewGuid()}", CourseTestClient.Update(course), CourseTestClient.Json)).StatusCode);
    }

    [Theory]
    [InlineData("duplicate", HttpStatusCode.Conflict)]
    [InlineData("foreign", HttpStatusCode.Conflict)]
    [InlineData("emptyId", HttpStatusCode.Conflict)]
    [InlineData("title", HttpStatusCode.BadRequest)]
    [InlineData("overflow", HttpStatusCode.BadRequest)]
    [InlineData("nullModule", HttpStatusCode.BadRequest)]
    [InlineData("nullResource", HttpStatusCode.BadRequest)]
    [InlineData("nullModules", HttpStatusCode.BadRequest)]
    [InlineData("nullBody", HttpStatusCode.BadRequest)]
    public async Task Update_InvalidRequestsDoNotPersistChanges(string invalid, HttpStatusCode status)
    {
        using var factory = new LumenApiFactory();
        using var teacher = await CourseTestClient.LoginAsync(factory);
        var course = await CourseTestClient.PublishedAsync(teacher);
        var before = JsonSerializer.Serialize(course);
        var dto = CourseTestClient.Update(course);
        var first = dto.Modules[0];
        dto = invalid switch
        {
            "duplicate" => dto with { Modules = [first, first] },
            "foreign" => dto with { Modules = [first with { Id = Guid.NewGuid() }] },
            "emptyId" => dto with { Modules = [first with { Id = Guid.Empty }] },
            "title" => dto with { Title = new string('x', 201) },
            "overflow" => dto with { Modules = [first with { DurationMinutes = int.MaxValue }, dto.Modules[1]] },
            "nullModule" => dto with { Modules = [null!] },
            "nullResource" => dto with { Modules = [first with { Resources = [null!] }] },
            "nullModules" => dto with { Modules = null! },
            _ => dto
        };
        var json = invalid == "nullBody" ? "null" : JsonSerializer.Serialize(dto, CourseTestClient.Json);
        var response = await teacher.PutAsync($"/api/courses/{course.Id}", new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(before, JsonSerializer.Serialize(await CourseTestClient.GetAsync(teacher, course.Id)));
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        Assert.Equal(1, await db.CourseRevisions.CountAsync(x => x.CourseId == course.Id));
        Assert.False(await db.CourseReviews.AnyAsync(x => x.CourseRevision.CourseId == course.Id));
    }

    [Theory]
    [InlineData(CourseReviewDecision.Aprobada)]
    [InlineData(CourseReviewDecision.CambiosSolicitados)]
    [InlineData(CourseReviewDecision.Rechazada)]
    public async Task Review_PreservesPublicContentUntilApprovalAndRejectsLateDecision(CourseReviewDecision decision)
    {
        using var factory = new LumenApiFactory();
        using var teacher = await CourseTestClient.LoginAsync(factory);
        using var admin = await CourseTestClient.LoginAsync(factory, "admin@lumen.local");
        using var student = await CourseTestClient.LoginAsync(factory, "alumno@lumen.local");
        var course = await CourseTestClient.PublishedAsync(teacher);
        var published = await student.GetFromJsonAsync<PublishedCourseResponseDTO>($"/api/catalog/courses/{course.Id}", CourseTestClient.Json);
        var enrollment = (await student.GetFromJsonAsync<List<EnrollmentResponseDTO>>("/api/enrollments/mine", CourseTestClient.Json))!
            .Single(x => x.CourseId == course.Id);
        var contentBefore = await student.GetFromJsonAsync<PublishedCourseResponseDTO>($"/api/enrollments/{enrollment.Id}/course-content", CourseTestClient.Json);
        var progressBefore = await student.GetFromJsonAsync<ProgressResponseDTO>($"/api/enrollments/{enrollment.Id}/progress", CourseTestClient.Json);
        // Keep module identities; change approved content only after CU-04.
        var updated = await CourseTestClient.PutAsync(teacher, course.Id, CourseTestClient.Update(course) with
            { Title = "Versión nueva", Modules = course.Modules.Select(x => CourseTestClient.Module(x) with { Content = "Otro contenido" }).ToList() });
        var review = await CourseTestClient.SubmitAsync(teacher, course.Id);
        Assert.Equal(CourseReviewType.Modificacion, review.Type);
        Assert.Equal("EnRevision", (await CourseTestClient.GetAsync(teacher, course.Id)).Status);
        Assert.Equal(JsonSerializer.Serialize(published), JsonSerializer.Serialize(
            await student.GetFromJsonAsync<PublishedCourseResponseDTO>($"/api/catalog/courses/{course.Id}", CourseTestClient.Json)));
        Assert.Equal(JsonSerializer.Serialize(contentBefore), JsonSerializer.Serialize(
            await student.GetFromJsonAsync<PublishedCourseResponseDTO>($"/api/enrollments/{enrollment.Id}/course-content", CourseTestClient.Json)));
        Assert.Equal(JsonSerializer.Serialize(progressBefore), JsonSerializer.Serialize(
            await student.GetFromJsonAsync<ProgressResponseDTO>($"/api/enrollments/{enrollment.Id}/progress", CourseTestClient.Json)));
        var observation = decision == CourseReviewDecision.CambiosSolicitados ? "Corregir descripción" : null;
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/course-reviews/{review.Id}/decision", new CourseReviewDecisionDTO(decision, observation), CourseTestClient.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/course-reviews/{review.Id}/decision",
            new CourseReviewDecisionDTO(CourseReviewDecision.Aprobada, null), CourseTestClient.Json)).StatusCode);
        var contentAfter = await student.GetFromJsonAsync<PublishedCourseResponseDTO>($"/api/enrollments/{enrollment.Id}/course-content", CourseTestClient.Json);
        Assert.Equal(decision == CourseReviewDecision.Aprobada ? "Versión nueva" : course.Title, contentAfter!.Title);
        Assert.Equal(decision == CourseReviewDecision.Aprobada ? updated.WorkingVersion : course.WorkingVersion, contentAfter.Version);
        var progressAfter = await student.GetFromJsonAsync<ProgressResponseDTO>($"/api/enrollments/{enrollment.Id}/progress", CourseTestClient.Json);
        Assert.Equal(progressBefore!.CompletedModuleIds, progressAfter!.CompletedModuleIds);
        Assert.Equal(progressBefore.Percentage, progressAfter.Percentage);
        var mine = await teacher.GetFromJsonAsync<List<CourseReviewResponseDTO>>("/api/course-reviews/mine", CourseTestClient.Json);
        Assert.Contains(mine!, x => x.Id == review.Id && x.Decision == decision && x.Observation == observation);
    }

    [Fact]
    public async Task ChangesRequested_CanBeCorrectedAndResubmittedWithNewRequest()
    {
        using var factory = new LumenApiFactory();
        using var teacher = await CourseTestClient.LoginAsync(factory);
        using var admin = await CourseTestClient.LoginAsync(factory, "admin@lumen.local");
        var draft = await CourseTestClient.DraftAsync(teacher);
        var first = await CourseTestClient.SubmitAsync(teacher, draft.Id);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/course-reviews/{first.Id}/decision",
            new CourseReviewDecisionDTO(CourseReviewDecision.CambiosSolicitados, "Corregir"), CourseTestClient.Json)).StatusCode);
        var corrections = await CourseTestClient.GetAsync(teacher, draft.Id);
        Assert.Equal("CambiosSolicitados", corrections.Status);
        var saved = await CourseTestClient.PutAsync(teacher, draft.Id, CourseTestClient.Update(corrections) with { Title = "Corregido" });
        Assert.Equal("Borrador", saved.Status); // Preserve the existing intermediate state.
        var second = await CourseTestClient.SubmitAsync(teacher, draft.Id);
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(first.RevisionId, second.RevisionId);
        var pending = await admin.GetFromJsonAsync<List<CourseReviewResponseDTO>>("/api/course-reviews", CourseTestClient.Json);
        Assert.Single(pending!, x => x.CourseId == draft.Id && x.Id == second.Id);
        Assert.Equal(HttpStatusCode.Conflict, (await teacher.PutAsJsonAsync($"/api/courses/{draft.Id}",
            CourseTestClient.Update(saved), CourseTestClient.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await teacher.PostAsync($"/api/courses/{draft.Id}/submit-review", null)).StatusCode);
    }

    [Fact]
    public async Task Draft_WithoutRequiredInformationCanBeSavedButCannotBeSubmitted()
    {
        using var factory = new LumenApiFactory();
        using var teacher = await CourseTestClient.LoginAsync(factory);
        var draft = await CourseTestClient.DraftAsync(teacher);
        var saved = await CourseTestClient.PutAsync(teacher, draft.Id, CourseTestClient.Update(draft) with { Title = "", Modules = [] });
        Assert.Equal("Borrador", saved.Status);
        Assert.Equal(0, saved.TotalDurationMinutes);
        Assert.Equal(HttpStatusCode.BadRequest, (await teacher.PostAsync($"/api/courses/{draft.Id}/submit-review", null)).StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<LumenDbContext>().CourseReviews.AnyAsync(x => x.CourseRevision.CourseId == draft.Id));
    }

    [Fact]
    public async Task ModuleUpdate_FailureOnFinalSaveRollsBackDeletionReorderingAndCourseClaim()
    {
        var failure = new FinalSaveFailure();
        using var factory = new LumenApiFactory(failure);
        using var teacher = await CourseTestClient.LoginAsync(factory);
        var course = await CourseTestClient.DraftAsync(teacher);
        var before = JsonSerializer.Serialize(await CourseTestClient.GetAsync(teacher, course.Id));
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
            var changes = CourseTestClient.Update(course) with { Title = "No debe guardarse",
                Modules = [CourseTestClient.Module(course.Modules[2]), CourseTestClient.Module(course.Modules[0])] };
            var service = new CourseService(new CourseRepository(db), new CourseTestUser(course.OwnerProfessorId!.Value));
            failure.Arm();
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(course.Id, changes));
        }
        failure.Disarm();
        Assert.Equal(before, JsonSerializer.Serialize(await CourseTestClient.GetAsync(teacher, course.Id)));
    }

    [Fact]
    public async Task EditingPendingModules_PreservesPublishedModulesAndRevisionIdentity()
    {
        using var factory = new LumenApiFactory();
        using var teacher = await CourseTestClient.LoginAsync(factory);
        using var student = await CourseTestClient.LoginAsync(factory, "alumno@lumen.local");
        var course = await CourseTestClient.PublishedAsync(teacher);
        var enrollment = (await student.GetFromJsonAsync<List<EnrollmentResponseDTO>>("/api/enrollments/mine", CourseTestClient.Json))!
            .Single(x => x.CourseId == course.Id);
        var contentBefore = await student.GetFromJsonAsync<PublishedCourseResponseDTO>($"/api/enrollments/{enrollment.Id}/course-content", CourseTestClient.Json);
        var pending = await CourseTestClient.PutAsync(teacher, course.Id, CourseTestClient.Update(course) with { Title = "Pendiente" });
        var changes = CourseTestClient.Update(pending) with
        {
            Modules = [CourseTestClient.Module(pending.Modules[2]) with { Content = "Corrección" },
                CourseTestClient.Module(pending.Modules[0]), new(null, "Nuevo", "Desc", 20, "Contenido", [])]
        };
        var saved = await CourseTestClient.PutAsync(teacher, course.Id, changes);
        Assert.Equal(pending.WorkingVersion, saved.WorkingVersion);
        Assert.Equal(course.PublishedVersion, saved.PublishedVersion);
        Assert.Equal(2, saved.WorkingVersion);
        Assert.Equal(290, saved.TotalDurationMinutes);
        Assert.Equal(JsonSerializer.Serialize(contentBefore), JsonSerializer.Serialize(
            await student.GetFromJsonAsync<PublishedCourseResponseDTO>($"/api/enrollments/{enrollment.Id}/course-content", CourseTestClient.Json)));
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        Assert.Equal(2, await db.CourseRevisions.CountAsync(x => x.CourseId == course.Id));
        var entity = await db.Courses.Include(x => x.PublishedRevision).ThenInclude(x => x!.Modules)
            .SingleAsync(x => x.Id == course.Id);
        Assert.Equal(3, entity.PublishedRevision!.Modules.Count);
        Assert.Equal(course.Modules.Select(x => x.Id).OrderBy(x => x), entity.PublishedRevision.Modules.Select(x => x.Id).OrderBy(x => x));
    }

    [Fact]
    public async Task EnrollmentDuringUpdate_UsesPublicVersionWithoutPersistingCourseGraph()
    {
        using var factory = new LumenApiFactory();
        using var teacher = await CourseTestClient.LoginAsync(factory);
        var course = await CourseTestClient.PublishedAsync(teacher);
        await CourseTestClient.PutAsync(teacher, course.Id, CourseTestClient.Update(course) with { Title = "Cambios sin aprobar" });
        var review = await CourseTestClient.SubmitAsync(teacher, course.Id);
        using var registration = factory.CreateClient();
        var email = $"cu05-{Guid.NewGuid():N}@lumen.local";
        Assert.Equal(HttpStatusCode.Created, (await registration.PostAsJsonAsync("/api/auth/register",
            new UserCreateDTO(email, DbInitializer.DemoPassword, UserRole.Alumno, "Alumno", "Nuevo", "12345678",
                "123", "Dirección", "1000"), CourseTestClient.Json)).StatusCode);
        using var student = await CourseTestClient.LoginAsync(factory, email);
        var response = await student.PostAsJsonAsync("/api/enrollments", new EnrollmentCreateDTO(course.Id), CourseTestClient.Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var enrollment = await response.Content.ReadFromJsonAsync<EnrollmentResponseDTO>(CourseTestClient.Json);
        Assert.Equal(EnrollmentStatus.Pendiente, enrollment!.Status);
        Assert.Equal(course.Title, enrollment.CourseTitle);
        Assert.Equal(HttpStatusCode.Conflict, (await student.GetAsync($"/api/enrollments/{enrollment.Id}/course-content")).StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        Assert.Equal(2, await db.CourseRevisions.CountAsync(x => x.CourseId == course.Id));
        Assert.Equal(6, await db.CourseModules.CountAsync(x => x.Revision.CourseId == course.Id));
        Assert.Equal(review.RevisionId, (await db.Courses.SingleAsync(x => x.Id == course.Id)).WorkingRevisionId);
        Assert.Single(await db.CourseReviews.Where(x => x.CourseRevision.CourseId == course.Id).ToListAsync());
    }

    [Fact]
    public async Task NewRevision_FailureOnInsertRollsBackCourseTimestampAndPointers()
    {
        var failure = new FinalSaveFailure();
        using var factory = new LumenApiFactory(failure);
        using var teacher = await CourseTestClient.LoginAsync(factory);
        var course = await CourseTestClient.PublishedAsync(teacher);
        var before = JsonSerializer.Serialize(course);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
            var service = new CourseService(new CourseRepository(db), new CourseTestUser(course.OwnerProfessorId!.Value));
            failure.Arm(2);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(course.Id,
                CourseTestClient.Update(course) with { Title = "No debe guardarse" }));
        }
        failure.Disarm();
        Assert.Equal(before, JsonSerializer.Serialize(await CourseTestClient.GetAsync(teacher, course.Id)));
        await using var verification = factory.Services.CreateAsyncScope();
        var savedDb = verification.ServiceProvider.GetRequiredService<LumenDbContext>();
        Assert.Equal(1, await savedDb.CourseRevisions.CountAsync(x => x.CourseId == course.Id));
        Assert.Equal(3, await savedDb.CourseModules.CountAsync(x => x.Revision.CourseId == course.Id));
    }

    private sealed class FinalSaveFailure : SaveChangesInterceptor
    {
        private int saves;
        private bool armed;
        private int failAt;
        public void Arm(int failOnSave = 3) { saves = 0; armed = true; failAt = failOnSave; }
        public void Disarm() => armed = false;
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (armed && ++saves == failAt) throw new InvalidOperationException("Fallo de persistencia simulado");
            return ValueTask.FromResult(result);
        }
    }
}

internal sealed class CourseTestUser(Guid id) : ICurrentUser
{
    public Guid UserId => id;
    public UserRole Role => UserRole.Profesor;
    public bool IsAuthenticated => true;
}

internal static class CourseTestClient
{
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        { Converters = { new JsonStringEnumConverter() } };

    internal static async Task<HttpClient> LoginAsync(LumenApiFactory factory, string email = "profesor@lumen.local")
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO(email, DbInitializer.DemoPassword), Json);
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponseDTO>(Json);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
        return client;
    }

    internal static async Task<CourseResponseDTO> DraftAsync(HttpClient client)
    {
        var dto = new CourseCreateDTO("Curso", "Descripción", "Categoría", "Inicial", "Online", 20, [], [],
            [new("Primero", "Desc", 30, "Contenido", []), new("Segundo", "Desc", 45, "Contenido", []),
             new("Tercero", "Desc", 60, "Contenido", [])]);
        var response = await client.PostAsJsonAsync("/api/courses", dto, Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CourseResponseDTO>(Json))!;
    }

    internal static async Task<CourseResponseDTO> PublishedAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<List<CourseResponseDTO>>("/api/courses", Json))!
            .First(x => x.PublishedVersion.HasValue && x.Modules.Count == 3);

    internal static async Task<CourseResponseDTO> GetAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<CourseResponseDTO>($"/api/courses/{id}", Json))!;

    internal static CourseModuleUpdateDTO Module(CourseModuleResponseDTO module) =>
        new(module.Id, module.Name, module.Description, module.DurationMinutes, module.Content ?? "", module.Resources);

    internal static CourseUpdateDTO Update(CourseResponseDTO course) =>
        new(course.Title, course.Description, course.Category, course.Level, course.Modality, course.MaxCapacity,
            course.LearningObjectives, course.SuggestedPrerequisites, course.Modules.Select(Module).ToList());

    internal static async Task<CourseResponseDTO> PutAsync(HttpClient client, Guid id, CourseUpdateDTO update)
    {
        var response = await client.PutAsJsonAsync($"/api/courses/{id}", update, Json);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CourseResponseDTO>(Json))!;
    }

    internal static async Task<CourseReviewResponseDTO> SubmitAsync(HttpClient client, Guid id)
    {
        var response = await client.PostAsync($"/api/courses/{id}/submit-review", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CourseReviewResponseDTO>(Json))!;
    }
}

