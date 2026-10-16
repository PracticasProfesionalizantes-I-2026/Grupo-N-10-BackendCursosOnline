using Lumen.BusinessLogic.Abstractions;
using Lumen.BusinessLogic.Interfaces;
using Lumen.DataAccess.Entities;
using Lumen.DataAccess.Repositories;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Lumen.Shared.Exceptions;

namespace Lumen.BusinessLogic.Services;

public sealed class EnrollmentService(
    IEnrollmentRepository enrollmentRepository,
    ICourseRepository courseRepository,
    ICurrentUser currentUser) : IEnrollmentService
{
    public async Task<EnrollmentResponseDTO> CreateAsync(EnrollmentCreateDTO dto, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Role(currentUser, UserRole.Alumno);
        if (currentUser.UserId == Guid.Empty)
        {
            throw new ForbiddenOperationException("La identidad del alumno autenticado no es válida.");
        }
        if (dto.CourseId == Guid.Empty)
        {
            throw new LumenValidationException("El identificador del curso es obligatorio y no puede estar vacío.");
        }
        var course = await courseRepository.GetPublishedByIdAsync(dto.CourseId, cancellationToken);
        if (course is null)
        {
            await EnsureCourseExistsAsync(dto.CourseId, cancellationToken);
            throw new CourseStateConflictException("El curso no se encuentra publicado o no admite nuevas inscripciones.");
        }
        if (await enrollmentRepository.HasPendingOrApprovedAsync(currentUser.UserId, dto.CourseId, cancellationToken))
        {
            throw new DuplicateEnrollmentException();
        }
        var enrollment = new Enrollment
        {
            StudentId = currentUser.UserId,
            CourseId = course.Id,
            Status = EnrollmentStatus.Pendiente,
            RequestedAtUtc = DateTime.UtcNow
        };
        if (!await enrollmentRepository.TryCreateForPublishedCourseAsync(
                enrollment, CourseOperationalStatus.Activo, cancellationToken))
        {
            await EnsureCourseExistsAsync(dto.CourseId, cancellationToken);
            throw new CourseStateConflictException("El curso dejó de admitir nuevas inscripciones.");
        }
        enrollment.Course = course;
        return MapToResponseDTO(enrollment);
    }

    private async Task EnsureCourseExistsAsync(Guid courseId, CancellationToken cancellationToken)
    {
        if (await courseRepository.GetByIdAsync(courseId, cancellationToken) is null)
        {
            throw new CourseNotFoundException(courseId);
        }
    }

    public async Task<IReadOnlyList<EnrollmentResponseDTO>> GetMineAsync(CancellationToken cancellationToken = default)
    {
        ServiceGuard.Role(currentUser, UserRole.Alumno);
        var enrollments = await enrollmentRepository.GetByStudentAsync(currentUser.UserId, cancellationToken);
        return enrollments.Select(MapToResponseDTO).ToList();
    }

    public async Task<IReadOnlyList<EnrollmentResponseDTO>> GetAllAsync(
        EnrollmentStatus? status,
        CancellationToken cancellationToken = default)
    {
        ServiceGuard.Role(currentUser, UserRole.Administrador);
        var enrollments = await enrollmentRepository.GetAllAsync(status, cancellationToken);
        return enrollments.Select(MapToResponseDTO).ToList();
    }

    public async Task<EnrollmentResponseDTO> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Role(currentUser, UserRole.Alumno, UserRole.Administrador);
        var enrollment = await enrollmentRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new EnrollmentNotFoundException(id);
        if (currentUser.Role == UserRole.Alumno && enrollment.StudentId != currentUser.UserId)
        {
            throw new EnrollmentOwnershipException();
        }
        return MapToResponseDTO(enrollment);
    }

    public async Task<PublishedCourseResponseDTO> GetCourseContentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Role(currentUser, UserRole.Alumno);
        var enrollment = await enrollmentRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new EnrollmentNotFoundException(id);
        EnsureStudentOwns(enrollment);
        EnsureApproved(enrollment);
        return CourseService.MapToPublishedResponseDTO(enrollment.Course, includeContent: true);
    }

    public async Task<EnrollmentResponseDTO> ResolveAsync(
        Guid id,
        EnrollmentDecisionDTO dto,
        CancellationToken cancellationToken = default)
    {
        ServiceGuard.Role(currentUser, UserRole.Administrador);
        if (dto.Decision is not (EnrollmentStatus.Aprobada or EnrollmentStatus.Rechazada))
        {
            throw new LumenValidationException("La decisión debe ser Aprobada o Rechazada.");
        }
        var enrollment = await enrollmentRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new EnrollmentNotFoundException(id);
        if (enrollment.Status != EnrollmentStatus.Pendiente)
        {
            throw new EnrollmentStateConflictException("Solo puede resolverse una inscripción pendiente.");
        }
        var resolvedAtUtc = DateTime.UtcNow;
        if (!await enrollmentRepository.TryResolveAsync(id, EnrollmentStatus.Pendiente, dto.Decision,
                resolvedAtUtc, currentUser.UserId, cancellationToken))
        {
            throw new EnrollmentStateConflictException("La solicitud ya fue resuelta por otra operación.");
        }
        enrollment.Status = dto.Decision;
        enrollment.ResolvedAtUtc = resolvedAtUtc;
        enrollment.ResolvedByUserId = currentUser.UserId;
        return MapToResponseDTO(enrollment);
    }

    public async Task<EnrollmentResponseDTO> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Role(currentUser, UserRole.Alumno, UserRole.Administrador);
        var enrollment = await enrollmentRepository.GetByIdForUpdateAsync(id, cancellationToken)
            ?? throw new EnrollmentNotFoundException(id);
        if (currentUser.Role == UserRole.Alumno)
        {
            EnsureStudentOwns(enrollment);
        }
        EnsureApproved(enrollment);
        var canceledAtUtc = DateTime.UtcNow;
        if (!await enrollmentRepository.TryCancelAsync(id, EnrollmentStatus.Aprobada, canceledAtUtc,
                currentUser.UserId, cancellationToken))
        {
            throw new EnrollmentStateConflictException("La inscripción cambió durante la cancelación.");
        }
        enrollment.Status = EnrollmentStatus.Cancelada;
        enrollment.CanceledAtUtc = canceledAtUtc;
        enrollment.CanceledByUserId = currentUser.UserId;
        return MapToResponseDTO(enrollment);
    }

    private void EnsureStudentOwns(Enrollment enrollment)
    {
        if (enrollment.StudentId != currentUser.UserId)
        {
            throw new EnrollmentOwnershipException();
        }
    }

    private static void EnsureApproved(Enrollment enrollment)
    {
        if (enrollment.Status != EnrollmentStatus.Aprobada)
        {
            throw new EnrollmentStateConflictException("La operación requiere una inscripción aprobada.");
        }
    }

    private static EnrollmentResponseDTO MapToResponseDTO(Enrollment enrollment)
    {
        var title = enrollment.Course.PublishedRevision?.Title
            ?? enrollment.Course.WorkingRevision?.Title
            ?? string.Empty;
        return new EnrollmentResponseDTO(
            enrollment.Id, enrollment.StudentId, enrollment.CourseId, title, enrollment.Status,
            enrollment.RequestedAtUtc, enrollment.ResolvedAtUtc, enrollment.CanceledAtUtc);
    }
}

