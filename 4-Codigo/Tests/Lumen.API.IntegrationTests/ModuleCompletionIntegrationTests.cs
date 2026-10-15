using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lumen.DataAccess;
using Lumen.DataAccess.Entities;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Lumen.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.API.IntegrationTests;

public sealed class ModuleCompletionIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Theory]
    [InlineData(3, 0, 33, ProgressStatus.EnProgreso)]
    [InlineData(201, 0, 1, ProgressStatus.EnProgreso)]
    [InlineData(200, 198, 99, ProgressStatus.EnProgreso)]
    [InlineData(200, 199, 100, ProgressStatus.Completado)]
    public async Task CompleteModule_PersistsCompletionAndReturnsUpdatedProgress(
        int total, int completed, int expectedPercentage, ProgressStatus expectedStatus)
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        var scenario = await PrepareEnrollmentAsync(factory, total, completed);
        await AuthenticateAsync(client, "alumno@lumen.local");
        var moduleId = scenario.ModuleIds[completed];

        var response = await client.PutAsync(CompletionUrl(scenario.EnrollmentId, moduleId), null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var progress = await response.Content.ReadFromJsonAsync<ProgressResponseDTO>(JsonOptions);
        AssertProgress(progress, scenario, completed + 1, expectedPercentage, expectedStatus);
        Assert.Contains(moduleId, progress!.CompletedModuleIds);
        var saved = await ReadCompletionsAsync(factory, scenario.EnrollmentId);
        Assert.Equal(completed + 1, saved.Count);
        var completion = Assert.Single(saved, x => x.ModuleId == moduleId);
        Assert.NotEqual(default, completion.CompletedAtUtc);
        Assert.Equal(saved.Select(x => x.ModuleId).Order(), progress.CompletedModuleIds.Order());
        var queried = await client.GetFromJsonAsync<ProgressResponseDTO>(
            $"/api/enrollments/{scenario.EnrollmentId}/progress", JsonOptions);
        AssertProgress(queried, scenario, completed + 1, expectedPercentage, expectedStatus);
        Assert.Equal(progress.CompletedModuleIds.Order(), queried!.CompletedModuleIds.Order());
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        var enrollment = await context.Enrollments.AsNoTracking().SingleAsync(x => x.Id == scenario.EnrollmentId);
        var course = await context.Courses.AsNoTracking().SingleAsync(x => x.Id == scenario.CourseId);
        Assert.Equal(EnrollmentStatus.Aprobada, enrollment.Status);
        Assert.Equal(CourseOperationalStatus.Activo, course.OperationalStatus);
    }

    [Fact]
    public async Task CompleteModule_WhenRepeated_DoesNotDuplicateCompletionOrChangeItsTimestamp()
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        var scenario = await PrepareEnrollmentAsync(factory, 3, 1);
        await AuthenticateAsync(client, "alumno@lumen.local");
        var moduleId = scenario.ModuleIds[1];
        var first = await client.PutAsync(CompletionUrl(scenario.EnrollmentId, moduleId), null);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var original = Assert.Single(await ReadCompletionsAsync(factory, scenario.EnrollmentId), x => x.ModuleId == moduleId);

        var repeated = await client.PutAsync(CompletionUrl(scenario.EnrollmentId, moduleId), null);

        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        var progress = await repeated.Content.ReadFromJsonAsync<ProgressResponseDTO>(JsonOptions);
        AssertProgress(progress, scenario, 2, 67, ProgressStatus.EnProgreso);
        var saved = await ReadCompletionsAsync(factory, scenario.EnrollmentId);
        Assert.Equal(2, saved.Count);
        Assert.Equal(original.CompletedAtUtc, Assert.Single(saved, x => x.ModuleId == moduleId).CompletedAtUtc);
        Assert.Equal(saved.Select(x => x.ModuleId).Order(), progress!.CompletedModuleIds.Order());
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("another-course")]
    [InlineData("working-revision")]
    public async Task CompleteModule_OutsidePublishedRevision_ReturnsConflictWithoutChangingProgress(string moduleSource)
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        var scenario = await PrepareEnrollmentAsync(factory, 3, 1);
        var moduleId = Guid.NewGuid();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
            if (moduleSource == "another-course")
            {
                moduleId = await context.CourseModules.Where(x => x.Revision.CourseId != scenario.CourseId)
                    .Select(x => x.Id).FirstAsync();
            }
            else if (moduleSource == "working-revision")
            {
                var course = await context.Courses.SingleAsync(x => x.Id == scenario.CourseId);
                var working = new CourseRevision
                {
                    Id = Guid.NewGuid(), CourseId = course.Id, Version = scenario.Version + 1,
                    Status = CourseRevisionStatus.Borrador, Title = "Cambios pendientes",
                    Modules = [new CourseModule
                    {
                        Id = moduleId, Order = 1, Name = "Módulo pendiente", Content = "Contenido no aprobado"
                    }]
                };
                context.CourseRevisions.Add(working);
                course.WorkingRevision = working;
                await context.SaveChangesAsync();
            }
        }
        await AuthenticateAsync(client, "alumno@lumen.local");

        var response = await client.PutAsync(CompletionUrl(scenario.EnrollmentId, moduleId), null);

        await AssertErrorAsync(response, HttpStatusCode.Conflict, nameof(ModuleCourseMismatchException));
        var saved = await ReadCompletionsAsync(factory, scenario.EnrollmentId);
        Assert.Equal(scenario.ModuleIds[0], Assert.Single(saved).ModuleId);
        var progress = await client.GetFromJsonAsync<ProgressResponseDTO>(
            $"/api/enrollments/{scenario.EnrollmentId}/progress", JsonOptions);
        AssertProgress(progress, scenario, 1, 33, ProgressStatus.EnProgreso);
        if (moduleSource == "working-revision")
        {
            var content = await client.GetFromJsonAsync<PublishedCourseResponseDTO>(
                $"/api/enrollments/{scenario.EnrollmentId}/course-content", JsonOptions);
            Assert.NotNull(content);
            Assert.Equal(scenario.Version, content.Version);
            Assert.DoesNotContain(content.Modules, x => x.Id == moduleId);
            Assert.Equal(scenario.ModuleIds.Order(), content.Modules.Select(x => x.Id).Order());
            var valid = await client.PutAsync(CompletionUrl(scenario.EnrollmentId, scenario.ModuleIds[1]), null);
            Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
            AssertProgress(await valid.Content.ReadFromJsonAsync<ProgressResponseDTO>(JsonOptions),
                scenario, 2, 67, ProgressStatus.EnProgreso);
        }
    }

    [Theory]
    [InlineData(EnrollmentStatus.Pendiente)]
    [InlineData(EnrollmentStatus.Rechazada)]
    [InlineData(EnrollmentStatus.Cancelada)]
    public async Task CompleteModule_WhenEnrollmentIsNotApproved_ReturnsConflictWithoutRecordingProgress(EnrollmentStatus status)
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        var scenario = await PrepareEnrollmentAsync(factory, 3, 1, status);
        await AuthenticateAsync(client, "alumno@lumen.local");

        var response = await client.PutAsync(CompletionUrl(scenario.EnrollmentId, scenario.ModuleIds[1]), null);

        await AssertErrorAsync(response, HttpStatusCode.Conflict, nameof(EnrollmentStateConflictException));
        var content = await client.GetAsync($"/api/enrollments/{scenario.EnrollmentId}/course-content");
        await AssertErrorAsync(content, HttpStatusCode.Conflict, nameof(EnrollmentStateConflictException));
        Assert.Equal(scenario.ModuleIds[0], Assert.Single(await ReadCompletionsAsync(factory, scenario.EnrollmentId)).ModuleId);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("profesor@lumen.local", HttpStatusCode.Forbidden)]
    [InlineData("admin@lumen.local", HttpStatusCode.Forbidden)]
    public async Task CompleteModule_RequiresAuthenticatedStudent(string? email, HttpStatusCode expectedStatus)
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        var scenario = await PrepareEnrollmentAsync(factory, 3, 0);
        if (email is not null)
            await AuthenticateAsync(client, email);

        var response = await client.PutAsync(CompletionUrl(scenario.EnrollmentId, scenario.ModuleIds[0]), null);

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Empty(await ReadCompletionsAsync(factory, scenario.EnrollmentId));
    }

    [Fact]
    public async Task CompleteModule_WhenEnrollmentBelongsToAnotherStudent_ReturnsForbidden()
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        var scenario = await PrepareEnrollmentAsync(factory, 3, 0);
        var registration = await client.PostAsJsonAsync("/api/auth/register",
            new UserCreateDTO("otro@lumen.local", DbInitializer.DemoPassword, UserRole.Alumno,
                "Otro", "Alumno", "99999999", "123456789", "Dirección", "1000"), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        await AuthenticateAsync(client, "otro@lumen.local");

        var response = await client.PutAsync(CompletionUrl(scenario.EnrollmentId, scenario.ModuleIds[0]), null);

        await AssertErrorAsync(response, HttpStatusCode.Forbidden, nameof(EnrollmentOwnershipException));
        Assert.Empty(await ReadCompletionsAsync(factory, scenario.EnrollmentId));
    }

    [Fact]
    public async Task CompleteModule_WhenEnrollmentDoesNotExist_ReturnsNotFound()
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        var scenario = await PrepareEnrollmentAsync(factory, 3, 0);
        await AuthenticateAsync(client, "alumno@lumen.local");

        var response = await client.PutAsync(CompletionUrl(Guid.NewGuid(), scenario.ModuleIds[0]), null);

        await AssertErrorAsync(response, HttpStatusCode.NotFound, nameof(EnrollmentNotFoundException));
        Assert.Empty(await ReadCompletionsAsync(factory, scenario.EnrollmentId));
    }

    [Fact]
    public async Task CourseContent_WhenReadWithoutCompletingModules_DoesNotRecordProgress()
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        var scenario = await PrepareEnrollmentAsync(factory, 3, 0);
        await AuthenticateAsync(client, "alumno@lumen.local");

        var content = await client.GetFromJsonAsync<PublishedCourseResponseDTO>(
            $"/api/enrollments/{scenario.EnrollmentId}/course-content", JsonOptions);

        Assert.NotNull(content);
        Assert.Equal(scenario.ModuleIds.Order(), content.Modules.Select(x => x.Id).Order());
        Assert.All(content.Modules, module => Assert.False(string.IsNullOrWhiteSpace(module.Content)));
        var progress = await client.GetFromJsonAsync<ProgressResponseDTO>(
            $"/api/enrollments/{scenario.EnrollmentId}/progress", JsonOptions);
        AssertProgress(progress, scenario, 0, 0, ProgressStatus.NoIniciado);
        Assert.Empty(progress!.CompletedModuleIds);
        Assert.Empty(await ReadCompletionsAsync(factory, scenario.EnrollmentId));
    }

    private static async Task<EnrollmentScenario> PrepareEnrollmentAsync(
        LumenApiFactory factory, int total, int completed, EnrollmentStatus status = EnrollmentStatus.Aprobada)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        var enrollment = await context.Enrollments.Include(x => x.ModuleCompletions)
            .Include(x => x.Course).ThenInclude(x => x.PublishedRevision).ThenInclude(x => x!.Modules)
            .SingleAsync(x => x.Status == EnrollmentStatus.Aprobada);
        context.EnrollmentModuleCompletions.RemoveRange(enrollment.ModuleCompletions);
        enrollment.ModuleCompletions.Clear();
        await context.SaveChangesAsync();
        var revision = enrollment.Course.PublishedRevision!;
        for (var i = revision.Modules.Count; i < total; i++)
        {
            revision.Modules.Add(new CourseModule
            {
                Id = Guid.NewGuid(), RevisionId = revision.Id, Order = i + 1,
                Name = $"Módulo {i + 1}", Description = "Descripción", Content = "Contenido", DurationMinutes = 1
            });
        }
        var moduleIds = revision.Modules.OrderBy(x => x.Order).Select(x => x.Id).ToArray();
        foreach (var moduleId in moduleIds.Take(completed))
        {
            enrollment.ModuleCompletions.Add(new EnrollmentModuleCompletion
            {
                EnrollmentId = enrollment.Id, ModuleId = moduleId, CompletedAtUtc = DateTime.UtcNow.AddDays(-1)
            });
        }
        enrollment.Status = status;
        revision.TotalDurationMinutes = revision.Modules.Sum(x => x.DurationMinutes);
        await context.SaveChangesAsync();
        return new EnrollmentScenario(enrollment.Id, enrollment.StudentId, enrollment.CourseId, revision.Version, moduleIds);
    }

    private static async Task<List<EnrollmentModuleCompletion>> ReadCompletionsAsync(LumenApiFactory factory, Guid enrollmentId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        return await context.EnrollmentModuleCompletions.AsNoTracking().Where(x => x.EnrollmentId == enrollmentId).ToListAsync();
    }

    private static void AssertProgress(ProgressResponseDTO? progress, EnrollmentScenario scenario,
        int completed, int percentage, ProgressStatus status)
    {
        Assert.NotNull(progress);
        Assert.Equal(scenario.EnrollmentId, progress.EnrollmentId);
        Assert.Equal(scenario.StudentId, progress.StudentId);
        Assert.Equal(scenario.CourseId, progress.CourseId);
        Assert.Equal(scenario.Version, progress.PublishedCourseVersion);
        Assert.Equal(scenario.ModuleIds.Length, progress.TotalModules);
        Assert.Equal(completed, progress.CompletedModules);
        Assert.Equal(completed, progress.CompletedModuleIds.Count);
        Assert.Equal(percentage, progress.Percentage);
        Assert.Equal(status, progress.Status);
    }

    private static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponseDTO>(JsonOptions);
        Assert.Equal(code, error?.Code);
    }

    private static string CompletionUrl(Guid enrollmentId, Guid moduleId) =>
        $"/api/enrollments/{enrollmentId}/modules/{moduleId}/completion";

    private static async Task AuthenticateAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDTO(email, DbInitializer.DemoPassword), JsonOptions);
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponseDTO>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
    }

    private sealed record EnrollmentScenario(Guid EnrollmentId, Guid StudentId, Guid CourseId, int Version, Guid[] ModuleIds);
}
