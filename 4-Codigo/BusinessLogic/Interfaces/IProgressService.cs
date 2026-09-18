using Lumen.Shared.DTOs;

namespace Lumen.BusinessLogic.Interfaces;

public interface IProgressService
{
    Task<ProgressResponseDTO> GetAsync(Guid enrollmentId, CancellationToken cancellationToken = default);
    Task<ProgressResponseDTO> CompleteModuleAsync(Guid enrollmentId, Guid moduleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentProgressResponseDTO>> GetCourseProgressAsync(Guid courseId, CancellationToken cancellationToken = default);
}

