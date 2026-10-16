using System.Collections.Concurrent;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Lumen.API.IntegrationTests;

internal static class ConcurrencyTestSupport
{
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(20);
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    internal static LumenApiFactory CreateFactory(ReadGate gate) => new()
    {
        ConfigureTestServices = services =>
        {
            services.AddScoped<IEnrollmentRepository>(provider => new GatedEnrollmentRepository(
                new EnrollmentRepository(provider.GetRequiredService<LumenDbContext>()),
                provider.GetRequiredService<LumenDbContext>(), gate));
            services.AddScoped<IUserRepository>(provider => new GatedUserRepository(
                new UserRepository(provider.GetRequiredService<LumenDbContext>()),
                provider.GetRequiredService<LumenDbContext>(), gate));
        }
    };

    internal static async Task<UserResponseDTO> AuthenticateAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDTO(email, DbInitializer.DemoPassword), Json);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var login = await response.Content.ReadFromJsonAsync<LoginResponseDTO>(Json);
        Assert.NotNull(login);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return login.User;
    }

    internal static async Task<Scenario> PrepareAsync(LumenApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        var enrollment = await context.Enrollments.Include(x => x.ModuleCompletions)
            .Include(x => x.Course).ThenInclude(x => x.PublishedRevision).ThenInclude(x => x!.Modules)
            .SingleAsync(x => x.Status == EnrollmentStatus.Aprobada);
        context.EnrollmentModuleCompletions.RemoveRange(enrollment.ModuleCompletions);
        await context.SaveChangesAsync();
        var published = enrollment.Course.PublishedRevision!;
        var modules = published.Modules.OrderBy(x => x.Order).Select(x => x.Id).ToArray();
        Assert.Equal(3, modules.Length);
        return new(enrollment.Id, enrollment.StudentId, enrollment.CourseId, published.Id, published.Version, modules);
    }

    internal static async Task<Snapshot> ReadAsync(LumenApiFactory factory, Scenario scenario)
    {
        // A fresh scope prevents EF's tracked graph from masquerading as persisted state.
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        var enrollment = await context.Enrollments.AsNoTracking().SingleAsync(x => x.Id == scenario.EnrollmentId);
        var publication = await context.Courses.AsNoTracking().Where(x => x.Id == scenario.CourseId)
            .Select(x => new { x.PublishedRevisionId, x.PublishedRevision!.Version }).SingleAsync();
        var completions = await context.EnrollmentModuleCompletions.AsNoTracking()
            .Where(x => x.EnrollmentId == scenario.EnrollmentId).OrderBy(x => x.ModuleId)
            .Select(x => new Completion(x.ModuleId, x.CompletedAtUtc)).ToArrayAsync();
        return new(enrollment.Status, enrollment.CanceledAtUtc, enrollment.CanceledByUserId,
            publication.PublishedRevisionId, publication.Version, completions);
    }

    internal static string CompletionUrl(Scenario scenario, Guid moduleId) =>
        $"/api/enrollments/{scenario.EnrollmentId}/modules/{moduleId}/completion";

    internal static async Task<Outcome> CaptureAsync(Func<Task<HttpResponseMessage>> send)
    {
        try { return new(await send(), null); }
        catch (Exception error) { return new(null, error); }
    }

    internal static void AssertHttp(Outcome outcome, HttpStatusCode expected)
    {
        // TestServer can propagate an unhandled exception instead of returning any HTTP response.
        // Never manufacture a 500 response or treat that exception as a successful request.
        Assert.True(outcome.Error is null,
            $"No HTTP response: {outcome.Error?.GetType().Name}; inner: {outcome.Error?.InnerException?.GetType().Name}");
        Assert.NotNull(outcome.Response);
        Assert.Equal(expected, outcome.Response.StatusCode);
    }

    internal static void Report(ITestOutputHelper output, string name, Outcome outcome) =>
        output.WriteLine($"{name}: HTTP {outcome.Response?.StatusCode.ToString() ?? "none"}; " +
            $"exception={outcome.Error?.GetType().Name ?? "none"}; inner={outcome.Error?.InnerException?.GetType().Name ?? "none"}");

    internal static async Task ObserveAsync(Task<Outcome> task)
    {
        // Always release the gate before calling this helper, including on assertion failures.
        // Observe the outstanding request before the temporary host/database is disposed.
        using var outcome = await task.WaitAsync(Deadline);
    }

    internal sealed record Scenario(Guid EnrollmentId, Guid StudentId, Guid CourseId,
        Guid PublicationId, int Version, Guid[] ModuleIds);
    internal sealed record Completion(Guid ModuleId, DateTime CompletedAtUtc);
    internal sealed record Snapshot(EnrollmentStatus Status, DateTime? CanceledAtUtc,
        Guid? CanceledByUserId, Guid? PublicationId, int Version, Completion[] Completions);
    internal sealed record Outcome(HttpResponseMessage? Response, Exception? Error) : IDisposable
    {
        public void Dispose() => Response?.Dispose();
    }

    internal sealed class ReadGate
    {
        private int armed;
        private Guid enrollmentId;
        private string? normalizedEmail;
        internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal ConcurrentDictionary<Guid, byte> Contexts { get; } = new();
        internal EnrollmentStatus? ReadStatus { get; private set; }
        internal Guid? ReadPublicationId { get; private set; }
        internal Guid[] ReadCompletedModules { get; private set; } = [];
        internal bool? ReadEmailExists { get; private set; }

        internal void ArmEnrollment(Guid id) { enrollmentId = id; Interlocked.Exchange(ref armed, 1); }
        internal void ArmEmail(string email) { normalizedEmail = email; Interlocked.Exchange(ref armed, 1); }

        internal async Task AfterEnrollmentReadAsync(Enrollment? entity, Guid contextId, CancellationToken ct)
        {
            if (entity?.Id != enrollmentId) return;
            Contexts.TryAdd(contextId, 0);
            if (Interlocked.Exchange(ref armed, 0) != 1) return;
            ReadStatus = entity.Status;
            ReadPublicationId = entity.Course.PublishedRevisionId;
            ReadCompletedModules = entity.ModuleCompletions.Select(x => x.ModuleId).ToArray();
            // The repository's real query has completed; no SQLite reader or transaction is held.
            Entered.TrySetResult();
            await Release.Task.WaitAsync(Deadline, ct);
        }

        internal async Task AfterEmailReadAsync(string email, bool exists, Guid contextId, CancellationToken ct)
        {
            if (email != normalizedEmail) return;
            Contexts.TryAdd(contextId, 0);
            if (Interlocked.Exchange(ref armed, 0) != 1) return;
            ReadEmailExists = exists;
            Entered.TrySetResult();
            await Release.Task.WaitAsync(Deadline, ct);
        }
    }

    private sealed class GatedEnrollmentRepository(IEnrollmentRepository inner, LumenDbContext context, ReadGate gate)
        : IEnrollmentRepository
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
            DateTime at, Guid actor, CancellationToken ct = default) => inner.TryResolveAsync(id, expectedStatus, decision, at, actor, ct);
        public Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken ct = default) => inner.GetByIdAsync(id, ct);
        public async Task<Enrollment?> GetByIdForUpdateAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await inner.GetByIdForUpdateAsync(id, ct);
            await gate.AfterEnrollmentReadAsync(entity, context.ContextId.InstanceId, ct);
            return entity;
        }
        public Task<IReadOnlyList<Enrollment>> GetByStudentAsync(Guid id, CancellationToken ct = default) => inner.GetByStudentAsync(id, ct);
        public Task<IReadOnlyList<Enrollment>> GetByCourseAsync(Guid id, CancellationToken ct = default) => inner.GetByCourseAsync(id, ct);
        public Task<IReadOnlyList<Enrollment>> GetAllAsync(EnrollmentStatus? status, CancellationToken ct = default) => inner.GetAllAsync(status, ct);
        public Task<bool> HasPendingOrApprovedAsync(Guid student, Guid course, CancellationToken ct = default) =>
            inner.HasPendingOrApprovedAsync(student, course, ct);
    }

    private sealed class GatedUserRepository(IUserRepository inner, LumenDbContext context, ReadGate gate) : IUserRepository
    {
        public Task<User> CreateAsync(User user, CancellationToken ct = default) => inner.CreateAsync(user, ct);
        public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) => inner.GetByIdAsync(id, ct);
        public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) => inner.GetByEmailAsync(email, ct);
        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default) => inner.GetAllAsync(ct);
        public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
        {
            var exists = await inner.EmailExistsAsync(email, ct);
            await gate.AfterEmailReadAsync(email, exists, context.ContextId.InstanceId, ct);
            return exists;
        }
    }
}
