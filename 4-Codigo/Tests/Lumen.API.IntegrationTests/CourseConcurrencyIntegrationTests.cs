using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lumen.DataAccess;
using Lumen.DataAccess.Entities;
using Lumen.DataAccess.Repositories;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.API.IntegrationTests;

public sealed class CourseConcurrencyIntegrationTests
{
    [Theory]
    [InlineData("submit", "submit")]
    [InlineData("publishedUpdate", "publishedUpdate")]
    [InlineData("edit", "submit")]
    [InlineData("submit", "edit")]
    [InlineData("edit", "edit")]
    public async Task CourseOperations_WithOverlappingReads_RejectStaleWrite(string firstOperation, string secondOperation)
    {
        var gate = new CourseReadGate();
        using var factory = Factory(gate);
        using var firstClient = await CourseTestClient.LoginAsync(factory);
        using var secondClient = await CourseTestClient.LoginAsync(factory);
        var course = firstOperation == "publishedUpdate"
            ? await CourseTestClient.PublishedAsync(firstClient) : await CourseTestClient.DraftAsync(firstClient);
        gate.Arm();
        var firstRequest = RequestAsync(firstClient, course, firstOperation, "Primera escritura");
        await gate.FirstRead.Task.WaitAsync(TimeSpan.FromSeconds(15));
        var secondRequest = RequestAsync(secondClient, course, secondOperation, "Escritura tardía");
        await gate.SecondRead.Task.WaitAsync(TimeSpan.FromSeconds(15));
        gate.AllowFirst.TrySetResult();
        var firstResponse = await firstRequest;
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        var committed = await SnapshotAsync(factory, course.Id);
        gate.AllowSecond.TrySetResult();
        var secondResponse = await secondRequest;
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        var error = await secondResponse.Content.ReadFromJsonAsync<ApiErrorResponseDTO>(CourseTestClient.Json);
        Assert.Equal("CourseStateConflictException", error!.Code);
        Assert.Equal(committed, await SnapshotAsync(factory, course.Id));

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        Assert.Equal(firstOperation == "publishedUpdate" ? 2 : 1,
            await db.CourseRevisions.CountAsync(x => x.CourseId == course.Id));
        Assert.Equal(firstOperation == "submit" ? 1 : 0,
            await db.CourseReviews.CountAsync(x => x.CourseRevision.CourseId == course.Id && x.Decision == null));
    }

    [Theory]
    [InlineData(CourseReviewDecision.Aprobada, CourseReviewDecision.Rechazada)]
    [InlineData(CourseReviewDecision.Rechazada, CourseReviewDecision.Aprobada)]
    [InlineData(CourseReviewDecision.Aprobada, CourseReviewDecision.Aprobada)]
    [InlineData(CourseReviewDecision.CambiosSolicitados, CourseReviewDecision.Aprobada)]
    public async Task Decisions_WithOverlappingReads_KeepFirstDecisionAndPublication(CourseReviewDecision winner, CourseReviewDecision late)
    {
        var gate = new CourseReadGate();
        using var factory = Factory(gate);
        using var teacher = await CourseTestClient.LoginAsync(factory);
        using var firstAdmin = await CourseTestClient.LoginAsync(factory, "admin@lumen.local");
        using var secondAdmin = await CourseTestClient.LoginAsync(factory, "admin@lumen.local");
        var published = await CourseTestClient.PublishedAsync(teacher);
        await CourseTestClient.PutAsync(teacher, published.Id, CourseTestClient.Update(published) with { Title = "Publicación nueva" });
        var review = await CourseTestClient.SubmitAsync(teacher, published.Id);
        gate.Arm();
        var firstRequest = firstAdmin.PostAsJsonAsync($"/api/course-reviews/{review.Id}/decision",
            new CourseReviewDecisionDTO(winner, winner == CourseReviewDecision.CambiosSolicitados ? "Corregir" : null), CourseTestClient.Json);
        await gate.FirstRead.Task.WaitAsync(TimeSpan.FromSeconds(15));
        var lateRequest = secondAdmin.PostAsJsonAsync($"/api/course-reviews/{review.Id}/decision",
            new CourseReviewDecisionDTO(late, null), CourseTestClient.Json);
        await gate.SecondRead.Task.WaitAsync(TimeSpan.FromSeconds(15));
        gate.AllowFirst.TrySetResult();
        Assert.Equal(HttpStatusCode.OK, (await firstRequest).StatusCode);
        var committed = await SnapshotAsync(factory, published.Id);
        gate.AllowSecond.TrySetResult();
        Assert.Equal(HttpStatusCode.Conflict, (await lateRequest).StatusCode);
        Assert.Equal(committed, await SnapshotAsync(factory, published.Id));
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        Assert.Equal(winner, (await db.CourseReviews.SingleAsync(x => x.Id == review.Id)).Decision);
        var course = await db.Courses.Include(x => x.PublishedRevision).Include(x => x.WorkingRevision)
            .SingleAsync(x => x.Id == published.Id);
        Assert.Equal(winner == CourseReviewDecision.Aprobada ? "Publicación nueva" : published.Title, course.PublishedRevision!.Title);
        Assert.Equal(CourseRevisionStatus.Publicado, course.PublishedRevision.Status);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task ModuleChanges_AndSubmission_WithOverlappingReads_PersistOnlyWinner(
        bool editReadsFirst, bool releaseTogether)
    {
        var gate = new CourseReadGate();
        using var factory = Factory(gate);
        using var editor = await CourseTestClient.LoginAsync(factory);
        using var submitter = await CourseTestClient.LoginAsync(factory);
        var course = await CourseTestClient.DraftAsync(editor);
        var changes = CourseTestClient.Update(course) with
        {
            Modules =
            [
                CourseTestClient.Module(course.Modules[2]) with
                {
                    Name = "Tercero corregido", Description = "Descripción corregida", DurationMinutes = 75,
                    Content = "Contenido corregido", Resources = ["revision.pdf"]
                },
                CourseTestClient.Module(course.Modules[0]),
                new(null, "Módulo agregado", "Descripción", 15, "Contenido agregado", ["nuevo.pdf"])
            ]
        };
        gate.Arm();
        Task<HttpResponseMessage> EditAsync() =>
            editor.PutAsJsonAsync($"/api/courses/{course.Id}", changes, CourseTestClient.Json);
        Task<HttpResponseMessage> SubmitAsync() =>
            submitter.PostAsync($"/api/courses/{course.Id}/submit-review", null);

        var firstRequest = editReadsFirst ? EditAsync() : SubmitAsync();
        await gate.FirstRead.Task.WaitAsync(TimeSpan.FromSeconds(15));
        var secondRequest = editReadsFirst ? SubmitAsync() : EditAsync();
        await gate.SecondRead.Task.WaitAsync(TimeSpan.FromSeconds(15));
        HttpResponseMessage firstResponse;
        HttpResponseMessage secondResponse;
        gate.AllowFirst.TrySetResult();
        if (releaseTogether)
        {
            // Both operations may write now; the asserted outcome does not depend on scheduling.
            gate.AllowSecond.TrySetResult();
            var responses = await Task.WhenAll(firstRequest, secondRequest);
            firstResponse = responses[0];
            secondResponse = responses[1];
        }
        else
        {
            firstResponse = await firstRequest;
            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
            var committed = await SnapshotAsync(factory, course.Id);
            gate.AllowSecond.TrySetResult();
            secondResponse = await secondRequest;
            Assert.Equal(committed, await SnapshotAsync(factory, course.Id));
        }
        using (firstResponse)
        using (secondResponse)
        {
            Assert.Equal([HttpStatusCode.OK, HttpStatusCode.Conflict],
                new[] { firstResponse.StatusCode, secondResponse.StatusCode }.OrderBy(x => x));
            var conflict = firstResponse.StatusCode == HttpStatusCode.Conflict ? firstResponse : secondResponse;
            var error = await conflict.Content.ReadFromJsonAsync<ApiErrorResponseDTO>(CourseTestClient.Json);
            Assert.Equal("CourseStateConflictException", error!.Code);
            var editResponse = editReadsFirst ? firstResponse : secondResponse;
            var editWon = editResponse.StatusCode == HttpStatusCode.OK;
            var persisted = await CourseTestClient.GetAsync(editor, course.Id);
            Assert.Equal(editWon ? "Borrador" : "EnRevision", persisted.Status);
            if (editWon)
            {
                Assert.Equal([course.Modules[2].Id, course.Modules[0].Id], persisted.Modules.Take(2).Select(x => x.Id));
                Assert.DoesNotContain(persisted.Modules, x => x.Id == course.Modules[1].Id);
                Assert.DoesNotContain(course.Modules, x => x.Id == persisted.Modules[2].Id);
                Assert.Equal("Módulo agregado", persisted.Modules[2].Name);
                Assert.Equal("Contenido agregado", persisted.Modules[2].Content);
                Assert.Equal(["nuevo.pdf"], persisted.Modules[2].Resources);
                Assert.Equal("Tercero corregido", persisted.Modules[0].Name);
                Assert.Equal("Descripción corregida", persisted.Modules[0].Description);
                Assert.Equal(75, persisted.Modules[0].DurationMinutes);
                Assert.Equal("Contenido corregido", persisted.Modules[0].Content);
                Assert.Equal(["revision.pdf"], persisted.Modules[0].Resources);
                Assert.Equal(120, persisted.TotalDurationMinutes);
            }
            else
            {
                Assert.Equal(JsonSerializer.Serialize(course.Modules), JsonSerializer.Serialize(persisted.Modules));
                Assert.Equal(course.TotalDurationMinutes, persisted.TotalDurationMinutes);
            }
            Assert.Equal([1, 2, 3], persisted.Modules.Select(x => x.Order));
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
            Assert.Equal(3, await db.CourseModules.CountAsync(x => x.Revision.CourseId == course.Id));
            Assert.Equal(1, await db.CourseRevisions.CountAsync(x => x.CourseId == course.Id));
            Assert.Equal(editWon ? 0 : 1, await db.CourseReviews.CountAsync(x => x.CourseRevision.CourseId == course.Id));
        }
    }

    private static LumenApiFactory Factory(CourseReadGate gate) => new()
    {
        ConfigureTestServices = services => services.AddScoped<ICourseRepository>(provider =>
            new GatedCourseRepository(new CourseRepository(provider.GetRequiredService<LumenDbContext>()), gate))
    };

    private static Task<HttpResponseMessage> RequestAsync(HttpClient client, CourseResponseDTO course, string operation, string title) =>
        operation == "submit" ? client.PostAsync($"/api/courses/{course.Id}/submit-review", null)
        : client.PutAsJsonAsync($"/api/courses/{course.Id}", CourseTestClient.Update(course) with { Title = title }, CourseTestClient.Json);

    private static async Task<string> SnapshotAsync(LumenApiFactory factory, Guid courseId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        return JsonSerializer.Serialize(new
        {
            Course = await db.Courses.Where(x => x.Id == courseId).Select(x => new { x.Id, x.CreatedByUserId,
                x.OwnerProfessorId, x.OperationalStatus, x.WorkingRevisionId, x.PublishedRevisionId, x.CreatedAtUtc, x.UpdatedAtUtc }).SingleAsync(),
            Revisions = await db.CourseRevisions.Where(x => x.CourseId == courseId).OrderBy(x => x.Version)
                .Select(x => new { x.Id, x.Version, x.Status, x.Title, x.Description, x.Category, x.Level,
                    x.Modality, x.MaxCapacity, x.TotalDurationMinutes, x.LearningObjectives, x.SuggestedPrerequisites,
                    x.CreatedAtUtc, x.UpdatedAtUtc, x.PublishedAtUtc }).ToListAsync(),
            Modules = await db.CourseModules.Where(x => x.Revision.CourseId == courseId).OrderBy(x => x.RevisionId).ThenBy(x => x.Order)
                .Select(x => new { x.RevisionId, x.Id, x.Order, x.Name, x.Description, x.DurationMinutes, x.Content, x.Resources }).ToListAsync(),
            Reviews = await db.CourseReviews.Where(x => x.CourseRevision.CourseId == courseId).OrderBy(x => x.Id)
                .Select(x => new { x.Id, x.CourseRevisionId, x.Type, x.Decision, x.Observation,
                    x.RequestedByUserId, x.RequestedAtUtc, x.ReviewedByUserId, x.ReviewedAtUtc }).ToListAsync()
        });
    }

    private sealed class CourseReadGate
    {
        private int reads;
        private bool armed;
        public TaskCompletionSource FirstRead { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource SecondRead { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource AllowFirst { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource AllowSecond { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void Arm() => armed = true;
        public async Task<T> AfterReadAsync<T>(T value, CancellationToken ct)
        {
            if (!armed) return value;
            var read = Interlocked.Increment(ref reads);
            if (read == 1)
            {
                FirstRead.TrySetResult();
                await AllowFirst.Task.WaitAsync(TimeSpan.FromSeconds(20), ct);
            }
            else if (read == 2)
            {
                SecondRead.TrySetResult();
                await AllowSecond.Task.WaitAsync(TimeSpan.FromSeconds(20), ct);
            }
            return value;
        }
    }

    private sealed class GatedCourseRepository(ICourseRepository inner, CourseReadGate gate) : ICourseRepository
    {
        public async Task<Course?> GetByIdForUpdateAsync(Guid id, CancellationToken ct = default) =>
            await gate.AfterReadAsync(await inner.GetByIdForUpdateAsync(id, ct), ct);
        public async Task<CourseReview?> GetReviewByIdForUpdateAsync(Guid id, CancellationToken ct = default) =>
            await gate.AfterReadAsync(await inner.GetReviewByIdForUpdateAsync(id, ct), ct);
        public Task<Course> CreateAsync(Course course, CourseRevision revision, CancellationToken ct = default) => inner.CreateAsync(course, revision, ct);
        public Task<CourseRevision> AddRevisionAsync(Course course, CourseRevision revision, CancellationToken ct = default) => inner.AddRevisionAsync(course, revision, ct);
        public Task UpdateWorkingRevisionAsync(Course course, CourseRevision changes, CancellationToken ct = default) => inner.UpdateWorkingRevisionAsync(course, changes, ct);
        public Task SaveAsync(CancellationToken ct = default) => inner.SaveAsync(ct);
        public Task<Course?> GetByIdAsync(Guid id, CancellationToken ct = default) => inner.GetByIdAsync(id, ct);
        public Task<Course?> GetPublishedByIdAsync(Guid id, CancellationToken ct = default) => inner.GetPublishedByIdAsync(id, ct);
        public Task<IReadOnlyList<Course>> GetPublishedAsync(CancellationToken ct = default) => inner.GetPublishedAsync(ct);
        public Task<IReadOnlyList<Course>> GetByOwnerAsync(Guid id, CancellationToken ct = default) => inner.GetByOwnerAsync(id, ct);
        public Task<IReadOnlyList<Course>> GetAllAsync(CancellationToken ct = default) => inner.GetAllAsync(ct);
        public Task<CourseReview> CreateReviewAsync(CourseReview review, CancellationToken ct = default) => inner.CreateReviewAsync(review, ct);
        public Task<IReadOnlyList<CourseReview>> GetPendingReviewsAsync(CancellationToken ct = default) => inner.GetPendingReviewsAsync(ct);
        public Task<IReadOnlyList<CourseReview>> GetReviewsByRequesterAsync(Guid id, CancellationToken ct = default) => inner.GetReviewsByRequesterAsync(id, ct);
    }
}

