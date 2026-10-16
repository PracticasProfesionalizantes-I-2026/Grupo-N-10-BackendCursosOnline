using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lumen.DataAccess;
using Lumen.DataAccess.Entities;
using Lumen.DataAccess.Repositories;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Lumen.Shared.Exceptions;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.API.IntegrationTests;

public sealed class EnrollmentResolutionConcurrencyIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public Task LateApproval_AfterRejectionAndNewRequest_Returns409AndPreservesResolution() =>
        RunResolutionRaceAsync(EnrollmentStatus.Rechazada, EnrollmentStatus.Aprobada, cancel: false);

    [Theory]
    [InlineData(EnrollmentStatus.Aprobada, EnrollmentStatus.Rechazada, false)]
    [InlineData(EnrollmentStatus.Aprobada, EnrollmentStatus.Aprobada, false)]
    [InlineData(EnrollmentStatus.Rechazada, EnrollmentStatus.Rechazada, false)]
    [InlineData(EnrollmentStatus.Aprobada, EnrollmentStatus.Aprobada, true)]
    public Task Resolve_ConcurrentDecisionDoesNotOverwriteWinningResolution(
        EnrollmentStatus winningDecision, EnrollmentStatus lateDecision, bool cancel) =>
        RunResolutionRaceAsync(winningDecision, lateDecision, cancel);

    [Fact]
    public async Task Resolve_UnrelatedForeignKeyFailureIsNotTranslatedIntoBusinessConflict()
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        var pending = await context.Enrollments.AsNoTracking().SingleAsync(x => x.Status == EnrollmentStatus.Pendiente);
        var repository = scope.ServiceProvider.GetRequiredService<IEnrollmentRepository>();

        var error = await Assert.ThrowsAsync<SqliteException>(() => repository.TryResolveAsync(
            pending.Id, EnrollmentStatus.Pendiente, EnrollmentStatus.Aprobada, DateTime.UtcNow, Guid.NewGuid()));

        Assert.Equal(787, error.SqliteExtendedErrorCode);
        var unchanged = await context.Enrollments.AsNoTracking().SingleAsync(x => x.Id == pending.Id);
        Assert.Equal(pending.Status, unchanged.Status);
        Assert.Equal(pending.ResolvedAtUtc, unchanged.ResolvedAtUtc);
        Assert.Equal(pending.ResolvedByUserId, unchanged.ResolvedByUserId);
    }

    private static async Task RunResolutionRaceAsync(
        EnrollmentStatus winningDecision, EnrollmentStatus lateDecision, bool cancel)
    {
        var gate = new EnrollmentReadGate();
        using var root = new LumenApiFactory();
        using var factory = root.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddScoped<IEnrollmentRepository>(provider => new DelayedEnrollmentRepository(
                new EnrollmentRepository(provider.GetRequiredService<LumenDbContext>()), gate))));
        using var student = factory.CreateClient();
        var studentUser = await AuthenticateAsync(student, "alumno@lumen.local");
        using var administrator = factory.CreateClient();
        var winningAdministrator = await AuthenticateAsync(administrator, "admin@lumen.local");
        var lateEmail = $"late-admin-{Guid.NewGuid():N}@lumen.local";
        var registration = await administrator.PostAsJsonAsync("/api/users/administrators", new AdministratorCreateDTO(
            lateEmail, DbInitializer.DemoPassword, "Administrador", "Atrasado", "87654321", "123456", "Dirección", "1000"), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        using var lateAdministrator = factory.CreateClient();
        var lateUser = await AuthenticateAsync(lateAdministrator, lateEmail);
        Assert.NotEqual(winningAdministrator.Id, lateUser.Id);
        var mine = await student.GetFromJsonAsync<List<EnrollmentResponseDTO>>("/api/enrollments/mine", JsonOptions);
        var original = Assert.Single(mine!, x => x.Status == EnrollmentStatus.Pendiente);

        // The real EF query has fully materialized and closed its reader before this gate waits.
        // This forces a stale PENDIENTE read without holding any database lock or using sleeps.
        gate.Arm(original.Id);
        var lateRequest = lateAdministrator.PostAsJsonAsync($"/api/enrollments/{original.Id}/decision",
            new EnrollmentDecisionDTO(lateDecision), JsonOptions);
        try
        {
            await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.Equal(EnrollmentStatus.Pendiente, gate.ReadStatus);
            var winningResponse = await administrator.PostAsJsonAsync($"/api/enrollments/{original.Id}/decision",
                new EnrollmentDecisionDTO(winningDecision), JsonOptions);
            Assert.Equal(HttpStatusCode.OK, winningResponse.StatusCode);
            if (cancel)
                Assert.Equal(HttpStatusCode.OK, (await student.PostAsync($"/api/enrollments/{original.Id}/cancel", null)).StatusCode);

            EnrollmentResponseDTO? replacement = null;
            if (winningDecision == EnrollmentStatus.Rechazada || cancel)
            {
                var create = await student.PostAsJsonAsync("/api/enrollments", new EnrollmentCreateDTO(original.CourseId));
                Assert.Equal(HttpStatusCode.Created, create.StatusCode);
                replacement = (await create.Content.ReadFromJsonAsync<EnrollmentResponseDTO>(JsonOptions))!;
                Assert.NotEqual(original.Id, replacement.Id);
            }
            var winner = await SnapshotAsync(factory, original.Id);
            Assert.Equal(winningAdministrator.Id, winner.ResolvedByUserId);
            if (cancel) Assert.Equal(studentUser.Id, winner.CanceledByUserId);
            gate.Release.TrySetResult();

            var response = await lateRequest;

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var error = (await response.Content.ReadFromJsonAsync<ApiErrorResponseDTO>(JsonOptions))!;
            Assert.Equal(nameof(EnrollmentStateConflictException), error.Code);
            Assert.Equal(winner, await SnapshotAsync(factory, original.Id));
            var detail = await student.GetFromJsonAsync<EnrollmentResponseDTO>($"/api/enrollments/{original.Id}", JsonOptions);
            Assert.Equal(winner.Status, detail!.Status);
            if (replacement is not null)
            {
                Assert.Equal(replacement, await student.GetFromJsonAsync<EnrollmentResponseDTO>(
                    $"/api/enrollments/{replacement.Id}", JsonOptions));
                Assert.Equal(HttpStatusCode.Conflict,
                    (await student.GetAsync($"/api/enrollments/{replacement.Id}/course-content")).StatusCode);
            }
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
            Assert.Equal(1, await context.Enrollments.CountAsync(x => x.StudentId == original.StudentId &&
                x.CourseId == original.CourseId && (x.Status == EnrollmentStatus.Pendiente || x.Status == EnrollmentStatus.Aprobada)));
        }
        finally
        {
            gate.Release.TrySetResult();
            // Observe an already-failing request before the isolated host is disposed.
            try { await lateRequest; } catch { }
        }
    }

    private static async Task<UserResponseDTO> AuthenticateAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO(email, DbInitializer.DemoPassword), JsonOptions);
        response.EnsureSuccessStatusCode();
        var login = (await response.Content.ReadFromJsonAsync<LoginResponseDTO>(JsonOptions))!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return login.User;
    }

    private static async Task<EnrollmentSnapshot> SnapshotAsync(WebApplicationFactory<Program> factory, Guid id)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var entity = await scope.ServiceProvider.GetRequiredService<LumenDbContext>().Enrollments.AsNoTracking().SingleAsync(x => x.Id == id);
        return new(entity.Status, entity.ResolvedAtUtc, entity.ResolvedByUserId, entity.CanceledAtUtc, entity.CanceledByUserId);
    }

    private sealed record EnrollmentSnapshot(EnrollmentStatus Status, DateTime? ResolvedAtUtc,
        Guid? ResolvedByUserId, DateTime? CanceledAtUtc, Guid? CanceledByUserId);

    private sealed class EnrollmentReadGate
    {
        private int armed;
        private Guid target;
        public EnrollmentStatus? ReadStatus { get; private set; }
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void Arm(Guid id) { target = id; Interlocked.Exchange(ref armed, 1); }

        public async Task<Enrollment?> AfterReadAsync(Enrollment? entity, CancellationToken cancellationToken)
        {
            if (entity?.Id == target && Interlocked.Exchange(ref armed, 0) == 1)
            {
                ReadStatus = entity.Status;
                Entered.TrySetResult();
                await Release.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
            }
            return entity;
        }
    }

    private sealed class DelayedEnrollmentRepository(IEnrollmentRepository inner, EnrollmentReadGate gate) : IEnrollmentRepository
    {
        public Task<Enrollment> CreateAsync(Enrollment entity, CancellationToken ct = default) => inner.CreateAsync(entity, ct);
        public Task<bool> TryCreateForPublishedCourseAsync(Enrollment entity, CourseOperationalStatus status, CancellationToken ct = default) =>
            inner.TryCreateForPublishedCourseAsync(entity, status, ct);
        public Task SaveAsync(CancellationToken ct = default) => inner.SaveAsync(ct);
        public Task<bool> TryCancelAsync(Guid id, EnrollmentStatus expectedStatus, DateTime at, Guid actor,
            CancellationToken ct = default) => inner.TryCancelAsync(id, expectedStatus, at, actor, ct);
        public Task<Enrollment?> TryCompleteModuleAsync(EnrollmentModuleCompletion completion, Guid student,
            EnrollmentStatus status, CancellationToken ct = default) => inner.TryCompleteModuleAsync(completion, student, status, ct);
        public Task<bool> TryResolveAsync(Guid id, EnrollmentStatus expectedStatus, EnrollmentStatus decision,
            DateTime resolvedAtUtc, Guid resolvedByUserId, CancellationToken ct = default) =>
            inner.TryResolveAsync(id, expectedStatus, decision, resolvedAtUtc, resolvedByUserId, ct);
        public async Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            await gate.AfterReadAsync(await inner.GetByIdAsync(id, ct), ct);
        public async Task<Enrollment?> GetByIdForUpdateAsync(Guid id, CancellationToken ct = default) =>
            await gate.AfterReadAsync(await inner.GetByIdForUpdateAsync(id, ct), ct);
        public Task<IReadOnlyList<Enrollment>> GetByStudentAsync(Guid id, CancellationToken ct = default) => inner.GetByStudentAsync(id, ct);
        public Task<IReadOnlyList<Enrollment>> GetByCourseAsync(Guid id, CancellationToken ct = default) => inner.GetByCourseAsync(id, ct);
        public Task<IReadOnlyList<Enrollment>> GetAllAsync(EnrollmentStatus? status, CancellationToken ct = default) => inner.GetAllAsync(status, ct);
        public Task<bool> HasPendingOrApprovedAsync(Guid studentId, Guid courseId, CancellationToken ct = default) =>
            inner.HasPendingOrApprovedAsync(studentId, courseId, ct);
    }
}
