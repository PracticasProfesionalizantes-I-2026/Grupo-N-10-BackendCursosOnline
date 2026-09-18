using Lumen.Shared.Enums;

namespace Lumen.Shared.DTOs;

public sealed record CourseModuleCreateDTO(
    string Name,
    string Description,
    int DurationMinutes,
    string Content,
    IReadOnlyList<string> Resources);

public sealed record CourseModuleUpdateDTO(
    Guid? Id,
    string Name,
    string Description,
    int DurationMinutes,
    string Content,
    IReadOnlyList<string> Resources);

public sealed record CourseModuleResponseDTO(
    Guid Id,
    int Order,
    string Name,
    string Description,
    int DurationMinutes,
    string? Content,
    IReadOnlyList<string> Resources);

public sealed record CourseCreateDTO(
    string Title,
    string Description,
    string Category,
    string Level,
    string Modality,
    int MaxCapacity,
    IReadOnlyList<string> LearningObjectives,
    IReadOnlyList<string> SuggestedPrerequisites,
    IReadOnlyList<CourseModuleCreateDTO> Modules);

public sealed record CourseUpdateDTO(
    string Title,
    string Description,
    string Category,
    string Level,
    string Modality,
    int MaxCapacity,
    IReadOnlyList<string> LearningObjectives,
    IReadOnlyList<string> SuggestedPrerequisites,
    IReadOnlyList<CourseModuleUpdateDTO> Modules);

public sealed record CourseResponseDTO(
    Guid Id,
    Guid CreatedByUserId,
    Guid? OwnerProfessorId,
    string Status,
    int WorkingVersion,
    int? PublishedVersion,
    string Title,
    string Description,
    string Category,
    string Level,
    string Modality,
    int MaxCapacity,
    int TotalDurationMinutes,
    IReadOnlyList<string> LearningObjectives,
    IReadOnlyList<string> SuggestedPrerequisites,
    IReadOnlyList<CourseModuleResponseDTO> Modules,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record PublishedCourseResponseDTO(
    Guid Id,
    int Version,
    string Title,
    string Description,
    string Category,
    string Level,
    string Modality,
    int MaxCapacity,
    int TotalDurationMinutes,
    IReadOnlyList<string> LearningObjectives,
    IReadOnlyList<string> SuggestedPrerequisites,
    IReadOnlyList<CourseModuleResponseDTO> Modules,
    DateTime PublishedAtUtc);

public sealed record CourseReviewResponseDTO(
    Guid Id,
    Guid CourseId,
    Guid RevisionId,
    int Version,
    CourseReviewType Type,
    CourseReviewDecision? Decision,
    string? Observation,
    Guid RequestedByUserId,
    Guid? ReviewedByUserId,
    DateTime RequestedAtUtc,
    DateTime? ReviewedAtUtc);

public sealed record CourseReviewDecisionDTO(
    CourseReviewDecision Decision,
    string? Observation);

