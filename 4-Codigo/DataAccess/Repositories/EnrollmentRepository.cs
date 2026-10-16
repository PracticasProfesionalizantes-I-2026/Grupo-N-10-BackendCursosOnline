using Lumen.DataAccess.Entities;
using Lumen.Shared.Enums;
using Lumen.Shared.Exceptions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Lumen.DataAccess.Repositories;

public sealed class EnrollmentRepository(LumenDbContext context) : IEnrollmentRepository
{
    public async Task<Enrollment> CreateAsync(Enrollment enrollment, CancellationToken cancellationToken = default)
    {
        enrollment.Id = Guid.NewGuid();
        await context.Enrollments.AddAsync(enrollment, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return enrollment;
    }

    public Task SaveAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    public async Task<bool> TryResolveAsync(
        Guid id,
        EnrollmentStatus expectedStatus,
        EnrollmentStatus decision,
        DateTime resolvedAtUtc,
        Guid resolvedByUserId,
        CancellationToken cancellationToken = default)
    {
        // Check the expected state and write the resolution in the same UPDATE.
        // A previous decision or cancellation cannot be overwritten by a stale read.
        var updated = await context.Enrollments
            .Where(x => x.Id == id && x.Status == expectedStatus)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, decision)
                .SetProperty(x => x.ResolvedAtUtc, resolvedAtUtc)
                .SetProperty(x => x.ResolvedByUserId, resolvedByUserId), cancellationToken);
        return updated == 1;
    }

    public async Task<bool> TryCancelAsync(
        Guid id,
        EnrollmentStatus expectedStatus,
        DateTime canceledAtUtc,
        Guid canceledByUserId,
        CancellationToken cancellationToken = default)
    {
        var updated = await context.Enrollments
            .Where(x => x.Id == id && x.Status == expectedStatus)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, EnrollmentStatus.Cancelada)
                .SetProperty(x => x.CanceledAtUtc, canceledAtUtc)
                .SetProperty(x => x.CanceledByUserId, canceledByUserId), cancellationToken);
        return updated == 1;
    }

    public async Task<Enrollment?> TryCompleteModuleAsync(
        EnrollmentModuleCompletion completion,
        Guid studentId,
        EnrollmentStatus requiredStatus,
        CancellationToken cancellationToken = default)
    {
        var status = requiredStatus.ToString();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        // Guard and insert in one write, serialized with cancellations and publication changes.
        // Ignore only the completion's composite-key conflict; preserve its original timestamp.
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "EnrollmentModuleCompletions" ("EnrollmentId", "ModuleId", "CompletedAtUtc")
            SELECT e."Id", m."Id", {completion.CompletedAtUtc}
            FROM "Enrollments" AS e
            INNER JOIN "Courses" AS c ON c."Id" = e."CourseId"
            INNER JOIN "CourseModules" AS m
                ON m."RevisionId" = c."PublishedRevisionId" AND m."Id" = {completion.ModuleId}
            WHERE e."Id" = {completion.EnrollmentId}
              AND e."StudentId" = {studentId}
              AND e."Status" = {status}
            ON CONFLICT ("EnrollmentId", "ModuleId") DO NOTHING
            """, cancellationToken);
        // Read from SQLite, not the earlier tracked graph. Keep the write transaction until
        // this snapshot is materialized so the response includes preceding committed advances.
        var current = await ReadGraph().SingleOrDefaultAsync(x => x.Id == completion.EnrollmentId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return current;
    }

    public async Task<bool> TryCreateForPublishedCourseAsync(
        Enrollment enrollment,
        CourseOperationalStatus requiredStatus,
        CancellationToken cancellationToken = default)
    {
        enrollment.Id = Guid.NewGuid();
        var status = enrollment.Status.ToString();
        var operationalStatus = requiredStatus.ToString();
        try
        {
            // One statement checks the current course and inserts only the new row.
            // SQLite serializes this write with concurrent pause/finish operations.
            var inserted = await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "Enrollments" ("Id", "StudentId", "CourseId", "Status", "RequestedAtUtc")
                SELECT {enrollment.Id}, {enrollment.StudentId}, "Id", {status}, {enrollment.RequestedAtUtc}
                FROM "Courses"
                WHERE "Id" = {enrollment.CourseId}
                  AND "PublishedRevisionId" IS NOT NULL
                  AND "OperationalStatus" = {operationalStatus}
                """, cancellationToken);
            return inserted == 1;
        }
        catch (SqliteException ex) when (
            ex.SqliteExtendedErrorCode == 2067 &&
            ex.Message.Contains("Enrollments.StudentId, Enrollments.CourseId", StringComparison.Ordinal))
        {
            // Translate this constraint only; other persistence failures must remain visible.
            throw new DuplicateEnrollmentException();
        }
    }

    public Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        ReadGraph().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Enrollment?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        TrackedGraph().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Enrollment>> GetByStudentAsync(Guid studentId, CancellationToken cancellationToken = default) =>
        await ReadGraph().Where(x => x.StudentId == studentId).OrderByDescending(x => x.RequestedAtUtc).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Enrollment>> GetByCourseAsync(Guid courseId, CancellationToken cancellationToken = default) =>
        await ReadGraph().Where(x => x.CourseId == courseId).OrderBy(x => x.Student.LastName).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Enrollment>> GetAllAsync(EnrollmentStatus? status, CancellationToken cancellationToken = default)
    {
        var query = ReadGraph();
        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status);
        }
        return await query.OrderByDescending(x => x.RequestedAtUtc).ToListAsync(cancellationToken);
    }

    public Task<bool> HasPendingOrApprovedAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default) =>
        context.Enrollments.AsNoTracking().AnyAsync(
            x => x.StudentId == studentId && x.CourseId == courseId &&
                 (x.Status == EnrollmentStatus.Pendiente || x.Status == EnrollmentStatus.Aprobada),
            cancellationToken);

    private IQueryable<Enrollment> ReadGraph() =>
        context.Enrollments.AsNoTracking()
            .Include(x => x.Student)
            .Include(x => x.ModuleCompletions)
            .Include(x => x.Course).ThenInclude(x => x.PublishedRevision).ThenInclude(x => x!.Modules);

    private IQueryable<Enrollment> TrackedGraph() =>
        context.Enrollments
            .Include(x => x.Student)
            .Include(x => x.ModuleCompletions)
            .Include(x => x.Course).ThenInclude(x => x.PublishedRevision).ThenInclude(x => x!.Modules);
}

