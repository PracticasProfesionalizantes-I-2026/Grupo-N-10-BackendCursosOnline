using Lumen.Shared.Enums;

namespace Lumen.Shared.DTOs;

public sealed record EnrollmentCreateDTO(Guid CourseId);
public sealed record EnrollmentUpdateDTO(EnrollmentStatus Status);
public sealed record EnrollmentDecisionDTO(EnrollmentStatus Decision);

public sealed record EnrollmentResponseDTO(
    Guid Id,
    Guid StudentId,
    Guid CourseId,
    string CourseTitle,
    EnrollmentStatus Status,
    DateTime RequestedAtUtc,
    DateTime? ResolvedAtUtc,
    DateTime? CanceledAtUtc);

public sealed record ProgressResponseDTO(
    Guid EnrollmentId,
    Guid StudentId,
    Guid CourseId,
    int PublishedCourseVersion,
    int CompletedModules,
    int TotalModules,
    int Percentage,
    ProgressStatus Status,
    IReadOnlyList<Guid> CompletedModuleIds);

public sealed record StudentProgressResponseDTO(
    Guid StudentId,
    string StudentName,
    ProgressResponseDTO Progress);

