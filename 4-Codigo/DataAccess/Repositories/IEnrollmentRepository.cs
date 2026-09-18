using Lumen.DataAccess.Entities;
using Lumen.Shared.Enums;

namespace Lumen.DataAccess.Repositories;

public interface IEnrollmentRepository
{
    Task<Enrollment> CreateAsync(Enrollment enrollment, CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);
    Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Enrollment?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Enrollment>> GetByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Enrollment>> GetByCourseAsync(Guid courseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Enrollment>> GetAllAsync(EnrollmentStatus? status, CancellationToken cancellationToken = default);
    Task<bool> HasPendingOrApprovedAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default);
}

