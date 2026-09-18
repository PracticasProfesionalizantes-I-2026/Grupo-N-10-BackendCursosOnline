using Lumen.DataAccess.Entities;

namespace Lumen.DataAccess.Repositories;

public interface ICourseRepository
{
    Task<Course> CreateAsync(Course course, CourseRevision workingRevision, CancellationToken cancellationToken = default);
    Task<CourseRevision> AddRevisionAsync(Course course, CourseRevision revision, CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);
    Task<Course?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Course?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Course?> GetPublishedByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Course>> GetPublishedAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Course>> GetByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Course>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CourseReview> CreateReviewAsync(CourseReview review, CancellationToken cancellationToken = default);
    Task<CourseReview?> GetReviewByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CourseReview>> GetPendingReviewsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CourseReview>> GetReviewsByRequesterAsync(Guid userId, CancellationToken cancellationToken = default);
}

