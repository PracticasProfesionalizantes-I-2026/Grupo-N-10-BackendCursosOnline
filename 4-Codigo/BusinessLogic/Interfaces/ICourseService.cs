using Lumen.Shared.DTOs;

namespace Lumen.BusinessLogic.Interfaces;

public interface ICourseService
{
    Task<IReadOnlyList<PublishedCourseResponseDTO>> GetPublishedAsync(CancellationToken cancellationToken = default);
    Task<PublishedCourseResponseDTO> GetPublishedByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CourseResponseDTO>> GetManageableAsync(CancellationToken cancellationToken = default);
    Task<CourseResponseDTO> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CourseResponseDTO> CreateAsync(CourseCreateDTO dto, CancellationToken cancellationToken = default);
    Task<CourseResponseDTO> UpdateAsync(Guid id, CourseUpdateDTO dto, CancellationToken cancellationToken = default);
    Task<CourseReviewResponseDTO> SubmitForReviewAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CourseReviewResponseDTO>> GetPendingReviewsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CourseReviewResponseDTO>> GetMyReviewsAsync(CancellationToken cancellationToken = default);
    Task<CourseReviewResponseDTO> ResolveReviewAsync(Guid reviewId, CourseReviewDecisionDTO dto, CancellationToken cancellationToken = default);
    Task<CourseResponseDTO> PauseAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CourseResponseDTO> ResumeAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CourseResponseDTO> FinishAsync(Guid id, CancellationToken cancellationToken = default);
}

