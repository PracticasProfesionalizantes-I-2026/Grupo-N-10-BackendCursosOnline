using Lumen.DataAccess.Entities;
using Lumen.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lumen.DataAccess.Repositories;

public sealed class CourseRepository(LumenDbContext context) : ICourseRepository
{
    public async Task<Course> CreateAsync(Course course, CourseRevision workingRevision, CancellationToken cancellationToken = default)
    {
        course.Id = Guid.NewGuid();
        course.WorkingRevisionId = null;
        course.PublishedRevisionId = null;
        workingRevision.Id = Guid.NewGuid();
        workingRevision.CourseId = course.Id;
        AssignModuleIds(workingRevision);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await context.Courses.AddAsync(course, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await context.CourseRevisions.AddAsync(workingRevision, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        course.WorkingRevisionId = workingRevision.Id;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        course.WorkingRevision = workingRevision;
        return course;
    }

    public async Task<CourseRevision> AddRevisionAsync(Course course, CourseRevision revision, CancellationToken cancellationToken = default)
    {
        revision.Id = Guid.NewGuid();
        revision.CourseId = course.Id;
        AssignModuleIds(revision);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await context.CourseRevisions.AddAsync(revision, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        course.WorkingRevisionId = revision.Id;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        course.WorkingRevision = revision;
        return revision;
    }

    public Task SaveAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    public Task<Course?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        ReadGraph().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Course?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        TrackedGraph().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Course?> GetPublishedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        PublishedGraph().SingleOrDefaultAsync(
            x => x.Id == id && x.PublishedRevisionId != null && x.OperationalStatus == CourseOperationalStatus.Activo,
            cancellationToken);

    public async Task<IReadOnlyList<Course>> GetPublishedAsync(CancellationToken cancellationToken = default) =>
        await PublishedGraph()
            .Where(x => x.PublishedRevisionId != null && x.OperationalStatus == CourseOperationalStatus.Activo)
            .OrderByDescending(x => x.PublishedRevision!.PublishedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Course>> GetByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default) =>
        await ReadGraph().Where(x => x.OwnerProfessorId == ownerId).OrderByDescending(x => x.UpdatedAtUtc).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Course>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await ReadGraph().OrderByDescending(x => x.UpdatedAtUtc).ToListAsync(cancellationToken);

    public async Task<CourseReview> CreateReviewAsync(CourseReview review, CancellationToken cancellationToken = default)
    {
        review.Id = Guid.NewGuid();
        await context.CourseReviews.AddAsync(review, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return review;
    }

    public Task<CourseReview?> GetReviewByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.CourseReviews
            .Include(x => x.CourseRevision).ThenInclude(x => x.Modules)
            .Include(x => x.CourseRevision).ThenInclude(x => x.Course)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CourseReview>> GetPendingReviewsAsync(CancellationToken cancellationToken = default) =>
        await context.CourseReviews.AsNoTracking()
            .Include(x => x.CourseRevision).ThenInclude(x => x.Course)
            .Where(x => x.Decision == null)
            .OrderBy(x => x.RequestedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CourseReview>> GetReviewsByRequesterAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.CourseReviews.AsNoTracking()
            .Include(x => x.CourseRevision).ThenInclude(x => x.Course)
            .Where(x => x.RequestedByUserId == userId)
            .OrderByDescending(x => x.RequestedAtUtc)
            .ToListAsync(cancellationToken);

    private IQueryable<Course> ReadGraph() =>
        context.Courses.AsNoTracking()
            .Include(x => x.WorkingRevision).ThenInclude(x => x!.Modules)
            .Include(x => x.PublishedRevision).ThenInclude(x => x!.Modules);

    private IQueryable<Course> TrackedGraph() =>
        context.Courses
            .Include(x => x.WorkingRevision).ThenInclude(x => x!.Modules)
            .Include(x => x.PublishedRevision).ThenInclude(x => x!.Modules);

    private IQueryable<Course> PublishedGraph() =>
        context.Courses.AsNoTracking()
            .Include(x => x.PublishedRevision).ThenInclude(x => x!.Modules);

    private static void AssignModuleIds(CourseRevision revision)
    {
        foreach (var module in revision.Modules)
        {
            module.RevisionId = revision.Id;
            if (module.Id == Guid.Empty)
            {
                module.Id = Guid.NewGuid();
            }
        }
    }
}

