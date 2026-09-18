using Lumen.BusinessLogic.Abstractions;
using Lumen.BusinessLogic.Interfaces;
using Lumen.DataAccess.Entities;
using Lumen.DataAccess.Repositories;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Lumen.Shared.Exceptions;

namespace Lumen.BusinessLogic.Services;

public sealed class ProgressService(
    IEnrollmentRepository enrollmentRepository,
    ICourseRepository courseRepository,
    ICurrentUser currentUser) : IProgressService
{
    public async Task<ProgressResponseDTO> GetAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Authenticated(currentUser);
        var enrollment = await enrollmentRepository.GetByIdAsync(enrollmentId, cancellationToken)
            ?? throw new EnrollmentNotFoundException(enrollmentId);
        EnsureCanRead(enrollment);
        EnsureApproved(enrollment);
        return MapToResponseDTO(enrollment);
    }

    public async Task<ProgressResponseDTO> CompleteModuleAsync(
        Guid enrollmentId,
        Guid moduleId,
        CancellationToken cancellationToken = default)
    {
        ServiceGuard.Role(currentUser, UserRole.Alumno);
        var enrollment = await enrollmentRepository.GetByIdForUpdateAsync(enrollmentId, cancellationToken)
            ?? throw new EnrollmentNotFoundException(enrollmentId);
        if (enrollment.StudentId != currentUser.UserId)
        {
            throw new EnrollmentOwnershipException();
        }
        EnsureApproved(enrollment);
        var published = enrollment.Course.PublishedRevision
            ?? throw new CourseStateConflictException("El curso no posee una versión publicada.");
        if (!published.Modules.Any(x => x.Id == moduleId))
        {
            throw new ModuleCourseMismatchException();
        }
        if (!enrollment.ModuleCompletions.Any(x => x.ModuleId == moduleId))
        {
            enrollment.ModuleCompletions.Add(new EnrollmentModuleCompletion
            {
                EnrollmentId = enrollment.Id,
                ModuleId = moduleId,
                CompletedAtUtc = DateTime.UtcNow
            });
            await enrollmentRepository.SaveAsync(cancellationToken);
        }
        return MapToResponseDTO(enrollment);
    }

    public async Task<IReadOnlyList<StudentProgressResponseDTO>> GetCourseProgressAsync(
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        ServiceGuard.Role(currentUser, UserRole.Profesor, UserRole.Administrador);
        var course = await courseRepository.GetByIdAsync(courseId, cancellationToken)
            ?? throw new CourseNotFoundException(courseId);
        if (currentUser.Role == UserRole.Profesor && course.OwnerProfessorId != currentUser.UserId)
        {
            throw new CourseOwnershipException();
        }
        var enrollments = await enrollmentRepository.GetByCourseAsync(courseId, cancellationToken);
        return enrollments
            .Where(x => x.Status == EnrollmentStatus.Aprobada)
            .Select(x => new StudentProgressResponseDTO(
                x.StudentId,
                $"{x.Student.FirstName} {x.Student.LastName}",
                MapToResponseDTO(x)))
            .ToList();
    }

    private void EnsureCanRead(Enrollment enrollment)
    {
        if (currentUser.Role == UserRole.Alumno && enrollment.StudentId != currentUser.UserId)
        {
            throw new EnrollmentOwnershipException();
        }
        if (currentUser.Role == UserRole.Profesor && enrollment.Course.OwnerProfessorId != currentUser.UserId)
        {
            throw new CourseOwnershipException();
        }
        if (currentUser.Role is not (UserRole.Alumno or UserRole.Profesor or UserRole.Administrador))
        {
            throw new ForbiddenOperationException();
        }
    }

    private static void EnsureApproved(Enrollment enrollment)
    {
        if (enrollment.Status != EnrollmentStatus.Aprobada)
        {
            throw new EnrollmentStateConflictException("El progreso solo está disponible para inscripciones aprobadas.");
        }
    }

    private static ProgressResponseDTO MapToResponseDTO(Enrollment enrollment)
    {
        var published = enrollment.Course.PublishedRevision
            ?? throw new CourseStateConflictException("El curso no posee una versión publicada.");
        var validModuleIds = published.Modules.Select(x => x.Id).ToHashSet();
        var completedIds = enrollment.ModuleCompletions
            .Select(x => x.ModuleId)
            .Where(validModuleIds.Contains)
            .Distinct()
            .ToList();
        var total = validModuleIds.Count;
        var percentage = total == 0 ? 0 : (int)Math.Round(completedIds.Count * 100d / total);
        var status = percentage switch
        {
            0 => ProgressStatus.NoIniciado,
            100 => ProgressStatus.Completado,
            _ => ProgressStatus.EnProgreso
        };
        return new ProgressResponseDTO(
            enrollment.Id, enrollment.StudentId, enrollment.CourseId, published.Version,
            completedIds.Count, total, percentage, status, completedIds);
    }
}

