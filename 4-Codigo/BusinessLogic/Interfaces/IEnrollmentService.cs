using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;

namespace Lumen.BusinessLogic.Interfaces;

public interface IEnrollmentService
{
    Task<EnrollmentResponseDTO> CreateAsync(EnrollmentCreateDTO dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EnrollmentResponseDTO>> GetMineAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EnrollmentResponseDTO>> GetAllAsync(EnrollmentStatus? status, CancellationToken cancellationToken = default);
    Task<EnrollmentResponseDTO> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PublishedCourseResponseDTO> GetCourseContentAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EnrollmentResponseDTO> ResolveAsync(Guid id, EnrollmentDecisionDTO dto, CancellationToken cancellationToken = default);
    Task<EnrollmentResponseDTO> CancelAsync(Guid id, CancellationToken cancellationToken = default);
}

