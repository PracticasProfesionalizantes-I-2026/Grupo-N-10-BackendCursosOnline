using System.Net;
using System.Net.Http.Json;
using Lumen.DataAccess;
using Lumen.DataAccess.Entities;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.API.IntegrationTests;

public sealed class CourseReviewHistoryIntegrationTests
{
    [Theory]
    [InlineData(CourseReviewDecision.Aprobada, null)]
    [InlineData(CourseReviewDecision.CambiosSolicitados, "Corregir contenidos")]
    public async Task RequestingAdministrator_CannotResolveOwnReview_AndAnotherAdministratorCan(
        CourseReviewDecision decision, string? observation)
    {
        using var factory = new LumenApiFactory();
        using var requester = await CourseTestClient.LoginAsync(factory, DemoAccounts.AdministratorEmail);
        var otherEmail = $"reviewer-{Guid.NewGuid():N}@lumen.local";
        using var created = await requester.PostAsJsonAsync("/api/users/administrators",
            new AdministratorCreateDTO(otherEmail, DbInitializer.DemoPassword, "Otra", "Auditora", "87654321",
                "11-1234-5678", "Dirección", "1000"), CourseTestClient.Json);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var reviewer = await CourseTestClient.LoginAsync(factory, otherEmail);
        var course = await CourseTestClient.DraftAsync(requester);
        var review = await CourseTestClient.SubmitAsync(requester, course.Id);

        using var forbidden = await requester.PostAsJsonAsync($"/api/course-reviews/{review.Id}/decision",
            new CourseReviewDecisionDTO(decision, observation), CourseTestClient.Json);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal("ForbiddenOperationException",
            (await forbidden.Content.ReadFromJsonAsync<ApiErrorResponseDTO>(CourseTestClient.Json))!.Code);
        var pending = await ReadHistoryAsync(requester, review.Id);
        Assert.Null(pending.Review.Decision);
        Assert.Null(pending.Review.ReviewedByUserId);
        Assert.Equal("EnRevision", (await CourseTestClient.GetAsync(requester, course.Id)).WorkingStatus);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
            Assert.Null((await db.CourseReviews.AsNoTracking().SingleAsync(x => x.Id == review.Id)).Decision);
            Assert.Null((await db.Courses.AsNoTracking().SingleAsync(x => x.Id == course.Id)).PublishedRevisionId);
        }

        await DecideAsync(reviewer, review.Id, decision, observation);
        Assert.Equal(decision, (await ReadHistoryAsync(reviewer, review.Id)).Review.Decision);
    }

    [Fact]
    public async Task ReviewRequester_IsCheckedPerSubmission_NotByOriginalCourseCreator()
    {
        using var factory = new LumenApiFactory();
        using var originalCreator = await CourseTestClient.LoginAsync(factory, DemoAccounts.AdministratorEmail);
        var otherEmail = $"editor-{Guid.NewGuid():N}@lumen.local";
        using var created = await originalCreator.PostAsJsonAsync("/api/users/administrators",
            new AdministratorCreateDTO(otherEmail, DbInitializer.DemoPassword, "Otro", "Editor", "87654321",
                "11-1234-5678", "Dirección", "1000"), CourseTestClient.Json);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var other = await CourseTestClient.LoginAsync(factory, otherEmail);
        var course = await CourseTestClient.DraftAsync(originalCreator);
        var first = await CourseTestClient.SubmitAsync(originalCreator, course.Id);
        await DecideAsync(other, first.Id, CourseReviewDecision.CambiosSolicitados, "Revisar");
        await CourseTestClient.PutAsync(other, course.Id, CourseTestClient.Update(course) with { Title = "Nueva versión" });
        var second = await CourseTestClient.SubmitAsync(other, course.Id);
        Assert.NotEqual(first.RequestedByUserId, second.RequestedByUserId);

        using var forbidden = await other.PostAsJsonAsync($"/api/course-reviews/{second.Id}/decision",
            new CourseReviewDecisionDTO(CourseReviewDecision.Aprobada, null), CourseTestClient.Json);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        await DecideAsync(originalCreator, second.Id, CourseReviewDecision.Aprobada, null);
        Assert.Equal(CourseReviewDecision.Aprobada, (await ReadHistoryAsync(originalCreator, second.Id)).Review.Decision);
    }

    [Fact]
    public async Task LegacyForeignPointers_DoNotExposeAnotherCourseOrEraseTheStoredRows()
    {
        using var factory = new LumenApiFactory();
        using var teacher = await CourseTestClient.LoginAsync(factory);
        using var student = await CourseTestClient.LoginAsync(factory, DemoAccounts.StudentEmail);
        var course = await CourseTestClient.PublishedAsync(teacher);
        var enrollment = (await student.GetFromJsonAsync<EnrollmentResponseDTO[]>("/api/enrollments/mine", CourseTestClient.Json))!
            .Single(x => x.CourseId == course.Id);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        var foreign = await db.Courses.Where(x => x.Id != course.Id && x.PublishedRevisionId != null)
            .Select(x => x.PublishedRevisionId).FirstAsync();

        // Reproduce a row from before the ownership trigger in this disposable database only.
        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER TR_Courses_RevisionOwner_Update");
        await db.Courses.Where(x => x.Id == course.Id).ExecuteUpdateAsync(setters => setters
            .SetProperty(x => x.PublishedRevisionId, foreign).SetProperty(x => x.WorkingRevisionId, foreign));
        using var management = await teacher.GetAsync($"/api/courses/{course.Id}");
        Assert.Equal(HttpStatusCode.NotFound, management.StatusCode);
        using var catalog = await student.GetAsync($"/api/catalog/courses/{course.Id}");
        Assert.Equal(HttpStatusCode.NotFound, catalog.StatusCode);
        using var content = await student.GetAsync($"/api/enrollments/{enrollment.Id}/course-content");
        Assert.Equal(HttpStatusCode.Conflict, content.StatusCode);
        var summary = (await student.GetFromJsonAsync<EnrollmentResponseDTO>($"/api/enrollments/{enrollment.Id}", CourseTestClient.Json))!;
        Assert.Empty(summary.CourseTitle);
        Assert.Equal(foreign, await db.Courses.Where(x => x.Id == course.Id).Select(x => x.PublishedRevisionId).SingleAsync());
    }

    [Fact]
    public async Task Republication_CountsUnchangedModulesAndKeepsOldCompletionsAsHistory()
    {
        using var factory = new LumenApiFactory();
        using var teacher = await CourseTestClient.LoginAsync(factory);
        using var admin = await CourseTestClient.LoginAsync(factory, DemoAccounts.AdministratorEmail);
        using var student = await CourseTestClient.LoginAsync(factory, DemoAccounts.StudentEmail);
        var original = await CourseTestClient.PublishedAsync(teacher);
        var enrollment = (await student.GetFromJsonAsync<EnrollmentResponseDTO[]>("/api/enrollments/mine", CourseTestClient.Json))!
            .Single(x => x.CourseId == original.Id);
        foreach (var module in original.Modules)
        {
            using var completion = await student.PutAsync($"/api/enrollments/{enrollment.Id}/modules/{module.Id}/completion", null);
            Assert.Equal(HttpStatusCode.OK, completion.StatusCode);
        }
        var changed = CourseTestClient.Module(original.Modules[1]) with { Content = "Contenido sustancialmente nuevo" };
        var update = CourseTestClient.Update(original) with
        {
            Modules = [changed, CourseTestClient.Module(original.Modules[0]), new(null, "Nuevo", "Desc", 20, "Contenido", [])]
        };
        var draft = await CourseTestClient.PutAsync(teacher, original.Id, update);
        Assert.NotEqual(original.Modules[1].Id, draft.Modules[0].Id);
        Assert.Equal(original.Modules[0].Id, draft.Modules[1].Id);
        var review = await CourseTestClient.SubmitAsync(teacher, original.Id);
        await DecideAsync(admin, review.Id, CourseReviewDecision.Aprobada, null);
        var progress = (await student.GetFromJsonAsync<ProgressResponseDTO>(
            $"/api/enrollments/{enrollment.Id}/progress", CourseTestClient.Json))!;
        Assert.Equal([original.Modules[0].Id], progress.CompletedModuleIds);
        Assert.Equal(3, progress.TotalModules);
        using var removed = await student.PutAsync($"/api/enrollments/{enrollment.Id}/modules/{original.Modules[2].Id}/completion", null);
        Assert.Equal(HttpStatusCode.Conflict, removed.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        var history = await db.EnrollmentModuleCompletions.Where(x => x.EnrollmentId == enrollment.Id).Select(x => x.ModuleId).ToListAsync();
        Assert.Equal(original.Modules.Select(x => x.Id).Order(), history.Order());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Recovery_CopiesRejectedWorkWithoutChangingTheReviewOrPublication(bool hasPublication)
    {
        using var factory = new LumenApiFactory();
        using var teacher = await CourseTestClient.LoginAsync(factory);
        using var admin = await CourseTestClient.LoginAsync(factory, DemoAccounts.AdministratorEmail);
        using var student = await CourseTestClient.LoginAsync(factory, DemoAccounts.StudentEmail);
        var original = hasPublication
            ? await CourseTestClient.PublishedAsync(teacher)
            : await CourseTestClient.DraftAsync(teacher);
        if (hasPublication)
            await CourseTestClient.PutAsync(teacher, original.Id,
                CourseTestClient.Update(original) with { Title = "Propuesta rechazada" });
        var review = await CourseTestClient.SubmitAsync(teacher, original.Id);
        await DecideAsync(admin, review.Id, CourseReviewDecision.Rechazada, "Corregir objetivos");
        var rejected = await CourseTestClient.GetAsync(teacher, original.Id);
        var tag = await CourseTestClient.ReadTagAsync(teacher, original.Id);

        using var recovered = await RecoverAsync(teacher, original.Id, tag);
        Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
        var draft = (await recovered.Content.ReadFromJsonAsync<CourseResponseDTO>(CourseTestClient.Json))!;
        Assert.Equal("Borrador", draft.WorkingStatus);
        Assert.Equal(rejected.WorkingVersion + 1, draft.WorkingVersion);
        Assert.Equal(rejected.Title, draft.Title);
        Assert.Equal(rejected.Modules.Select(x => x.Id), draft.Modules.Select(x => x.Id));
        Assert.Equal(original.PublishedVersion, draft.PublishedVersion);
        using var replay = await RecoverAsync(teacher, original.Id, tag);
        Assert.Equal(HttpStatusCode.PreconditionFailed, replay.StatusCode);
        using var duplicate = await RecoverAsync(teacher, original.Id, recovered.Headers.ETag!.ToString());
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var history = await ReadHistoryAsync(teacher, review.Id);
        Assert.Equal(CourseReviewDecision.Rechazada, history.Review.Decision);
        Assert.Equal("Corregir objetivos", history.Review.Observation);
        Assert.Equal(rejected.Title, history.Submission!.Title);
        Assert.Equal(original.PublishedVersion, history.Submission.PublishedVersion);
        using var catalog = await student.GetAsync($"/api/catalog/courses/{original.Id}");
        Assert.Equal(hasPublication ? HttpStatusCode.OK : HttpStatusCode.NotFound, catalog.StatusCode);
        if (hasPublication)
            Assert.Equal(original.Title,
                (await catalog.Content.ReadFromJsonAsync<PublishedCourseResponseDTO>(CourseTestClient.Json))!.Title);
    }

    [Fact]
    public async Task History_StoresEachSubmissionAndProtectsItFromChangesAndUnrelatedReaders()
    {
        using var factory = new LumenApiFactory();
        using var teacher = await CourseTestClient.LoginAsync(factory);
        using var admin = await CourseTestClient.LoginAsync(factory, DemoAccounts.AdministratorEmail);
        using var student = await CourseTestClient.LoginAsync(factory, DemoAccounts.StudentEmail);
        var course = await CourseTestClient.DraftAsync(teacher);
        var first = await CourseTestClient.SubmitAsync(teacher, course.Id);
        await DecideAsync(admin, first.Id, CourseReviewDecision.CambiosSolicitados, "Cambiar título");
        await CourseTestClient.PutAsync(teacher, course.Id, CourseTestClient.Update(course) with { Title = "Texto corregido" });
        var second = await CourseTestClient.SubmitAsync(teacher, course.Id);
        await DecideAsync(admin, second.Id, CourseReviewDecision.Aprobada, null);

        var initial = await ReadHistoryAsync(admin, first.Id);
        var latest = await ReadHistoryAsync(teacher, second.Id);
        Assert.Equal(first.RevisionId, second.RevisionId);
        Assert.Equal(course.Title, initial.Submission!.Title);
        Assert.Equal("Texto corregido", latest.Submission!.Title);
        Assert.Equal(CourseReviewDecision.Aprobada, latest.Review.Decision);
        Assert.Equal(latest.Review.Version, (await CourseTestClient.GetAsync(teacher, course.Id)).PublishedVersion);
        using var forbidden = await student.GetAsync($"/api/course-reviews/{first.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        var legacyReviewId = Guid.NewGuid();
        db.CourseReviews.Add(new CourseReview
        {
            Id = legacyReviewId,
            CourseRevisionId = first.RevisionId,
            RequestedByUserId = first.RequestedByUserId,
            RequestedAtUtc = first.RequestedAtUtc.AddDays(-1),
            Type = first.Type,
            Decision = CourseReviewDecision.Rechazada
        });
        await db.SaveChangesAsync();
        Assert.Null((await ReadHistoryAsync(admin, legacyReviewId)).Submission);
        await Assert.ThrowsAsync<SqliteException>(() => db.Set<CourseReviewSnapshot>()
            .Where(x => x.ReviewId == first.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.ContentJson, "{}")));
        await Assert.ThrowsAsync<SqliteException>(() => db.Set<CourseReviewSnapshot>()
            .Where(x => x.ReviewId == first.Id).ExecuteDeleteAsync());
        await db.GetService<IMigrator>().MigrateAsync("20261106212402_PreserveReviewSnapshots");
        Assert.Equal(course.Title, (await ReadHistoryAsync(admin, first.Id)).Submission!.Title);
        await db.Database.MigrateAsync();
        Assert.Contains("20261106220000_PreventReviewSnapshotReplacement", await db.Database.GetAppliedMigrationsAsync());
        await db.Database.OpenConnectionAsync();
        await db.Database.ExecuteSqlRawAsync("PRAGMA recursive_triggers=OFF;");
        await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT OR REPLACE INTO "CourseReviewSnapshots" ("ReviewId", "ContentJson")
            VALUES ({first.Id}, {"{}"})
            """));
        Assert.Equal(course.Title, (await ReadHistoryAsync(admin, first.Id)).Submission!.Title);
    }

    private static async Task DecideAsync(HttpClient admin, Guid id, CourseReviewDecision decision, string? note)
    {
        using var result = await admin.PostAsJsonAsync($"/api/course-reviews/{id}/decision",
            new CourseReviewDecisionDTO(decision, note), CourseTestClient.Json);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    private static async Task<CourseReviewSnapshotResponseDTO> ReadHistoryAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<CourseReviewSnapshotResponseDTO>($"/api/course-reviews/{id}", CourseTestClient.Json))!;

    private static async Task<HttpResponseMessage> RecoverAsync(HttpClient client, Guid id, string tag)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/courses/{id}/recover-draft");
        request.Headers.TryAddWithoutValidation("If-Match", tag);
        return await client.SendAsync(request);
    }
}
