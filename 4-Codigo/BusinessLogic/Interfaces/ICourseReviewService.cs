using Lumen.Shared.DTOs;

namespace Lumen.BusinessLogic.Interfaces;

public interface ICourseReviewService
{
    Task<CourseReviewResponseDTO> SubmitForReviewAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CourseReviewResponseDTO>> GetPendingReviewsAsync(CancellationToken cancellationToken = default, ListPage? page = null);
    Task<IReadOnlyList<CourseReviewResponseDTO>> GetMyReviewsAsync(CancellationToken cancellationToken = default, ListPage? page = null);
    Task<CourseReviewSnapshotResponseDTO> GetReviewSnapshotAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CourseReviewResponseDTO> ResolveReviewAsync(Guid reviewId, CourseReviewDecisionDTO dto, CancellationToken cancellationToken = default);
}
