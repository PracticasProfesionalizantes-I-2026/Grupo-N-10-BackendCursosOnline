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
        var course = await courseRepository.GetPublishedByIdAsync(dto.CourseId, cancellationToken)
            ?? throw new CourseStateConflictException("El curso no se encuentra publicado o no admite nuevas inscripciones.");
        if (await enrollmentRepository.HasPendingOrApprovedAsync(currentUser.UserId, dto.CourseId, cancellationToken))
        {
            throw new DuplicateEnrollmentException();
        }
        var enrollment = new Enrollment
        {
            StudentId = currentUser.UserId,
            CourseId = course.Id,
            Course = course,
            Status = EnrollmentStatus.Pendiente,
            RequestedAtUtc = DateTime.UtcNow
        };
        await enrollmentRepository.CreateAsync(enrollment, cancellationToken);
        return MapToResponseDTO(enrollment);
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
        var enrollment = await enrollmentRepository.GetByIdForUpdateAsync(id, cancellationToken)
            ?? throw new EnrollmentNotFoundException(id);
        if (enrollment.Status != EnrollmentStatus.Pendiente)
        {
            throw new EnrollmentStateConflictException("Solo puede resolverse una inscripción pendiente.");
        }
        enrollment.Status = dto.Decision;
        enrollment.ResolvedAtUtc = DateTime.UtcNow;
        enrollment.ResolvedByUserId = currentUser.UserId;
        await enrollmentRepository.SaveAsync(cancellationToken);
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
        enrollment.Status = EnrollmentStatus.Cancelada;
        enrollment.CanceledAtUtc = DateTime.UtcNow;
        enrollment.CanceledByUserId = currentUser.UserId;
        await enrollmentRepository.SaveAsync(cancellationToken);
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

