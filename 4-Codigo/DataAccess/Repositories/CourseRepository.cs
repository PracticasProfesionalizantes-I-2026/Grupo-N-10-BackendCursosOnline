using Lumen.DataAccess.Entities;
using Lumen.Shared.Enums;
using Lumen.Shared.Exceptions;
using Microsoft.Data.Sqlite;
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
        await SaveAsync(cancellationToken);
        await context.CourseRevisions.AddAsync(workingRevision, cancellationToken);
        await SaveAsync(cancellationToken);
        course.WorkingRevisionId = workingRevision.Id;
        await SaveAsync(cancellationToken);
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
        // Claim the course using its original concurrency values before inserting a version.
        await SaveAsync(cancellationToken);
        await context.CourseRevisions.AddAsync(revision, cancellationToken);
        await SaveAsync(cancellationToken);
        course.WorkingRevisionId = revision.Id;
        await SaveAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        course.WorkingRevision = revision;
        return revision;
    }

    public async Task UpdateWorkingRevisionAsync(Course course, CourseRevision changes, CancellationToken cancellationToken = default)
    {
        var working = course.WorkingRevision!;
        var existing = working.Modules.ToDictionary(x => x.Id);
        var retainedIds = changes.Modules.Where(x => x.Id != Guid.Empty).Select(x => x.Id).ToHashSet();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        // Save the service's course timestamp first: a stale edit must fail before touching modules.
        await SaveAsync(cancellationToken);
        foreach (var omitted in working.Modules.Where(x => !retainedIds.Contains(x.Id)).ToList())
        {
            context.CourseModules.Remove(omitted);
            working.Modules.Remove(omitted);
        }
        // Vacate all positive positions to allow swaps under the unique (RevisionId, Order) index.
        var temporaryOrder = -1;
        foreach (var module in working.Modules)
        {
            module.Order = temporaryOrder--;
        }
        await SaveAsync(cancellationToken);

        foreach (var input in changes.Modules)
        {
            if (input.Id == Guid.Empty)
            {
                input.Id = Guid.NewGuid();
                input.RevisionId = working.Id;
                working.Modules.Add(input);
            }
            else
            {
                var module = existing[input.Id];
                module.Order = input.Order;
                module.Name = input.Name;
                module.Description = input.Description;
                module.DurationMinutes = input.DurationMinutes;
                module.Content = input.Content;
                module.Resources = input.Resources;
            }
        }
        working.Title = changes.Title;
        working.Description = changes.Description;
        working.Category = changes.Category;
        working.Level = changes.Level;
        working.Modality = changes.Modality;
        working.MaxCapacity = changes.MaxCapacity;
        working.LearningObjectives = changes.LearningObjectives;
        working.SuggestedPrerequisites = changes.SuggestedPrerequisites;
        working.TotalDurationMinutes = changes.TotalDurationMinutes;
        working.Status = changes.Status;
        working.UpdatedAtUtc = changes.UpdatedAtUtc;
        await SaveAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new CourseStateConflictException("El curso o la solicitud cambió durante la operación. Volvé a consultar su estado.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException sqlite &&
            sqlite.SqliteExtendedErrorCode == 2067 &&
            sqlite.Message.Contains("CourseRevisions.CourseId, CourseRevisions.Version", StringComparison.Ordinal))
        {
            throw new CourseStateConflictException("Otra operación ya creó la siguiente versión del curso.");
        }
    }

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
        await SaveAsync(cancellationToken);
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
            .Where(x => x.Decision == null && x.CourseRevision.Status == CourseRevisionStatus.EnRevision &&
                x.CourseRevision.Course.WorkingRevisionId == x.CourseRevisionId)
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

