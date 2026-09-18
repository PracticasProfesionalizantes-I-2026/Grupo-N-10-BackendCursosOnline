using Lumen.DataAccess.Entities;
using Lumen.Shared.Enums;
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

