using System.Text.Json;
using Lumen.DataAccess.Entities;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Lumen.Shared.Exceptions;

namespace Lumen.BusinessLogic.Services;

// Persisted independently of the public HTTP response contract. Versionless JSON is the legacy DTO format.
internal sealed record CourseReviewSnapshotPayload(
    int SchemaVersion, Guid Id, Guid CreatedByUserId, Guid? OwnerProfessorId,
    string Status, string WorkingStatus, int WorkingVersion, int? PublishedVersion,
    string Title, string Description, string Category, string Level, string Modality,
    int MaxCapacity, int TotalDurationMinutes, IReadOnlyList<string> LearningObjectives,
    IReadOnlyList<string> SuggestedPrerequisites, IReadOnlyList<CourseReviewSnapshotModule> Modules,
    DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

internal sealed record CourseReviewSnapshotModule(
    Guid Id, int Order, string Name, string Description, int DurationMinutes,
    string Content, IReadOnlyList<string> Resources);

internal static class CourseReviewSnapshotCodec
{
    internal static string Serialize(Course course)
    {
        var revision = course.WorkingRevision ?? throw new CourseStateConflictException("El curso no posee una versión de trabajo.");
        var status = course.OperationalStatus switch
        {
            CourseOperationalStatus.Pausado => "Pausado",
            CourseOperationalStatus.Finalizado => "Finalizado",
            _ => revision.Status.ToString()
        };
        var payload = new CourseReviewSnapshotPayload(
            1, course.Id, course.CreatedByUserId, course.OwnerProfessorId, status, revision.Status.ToString(),
            revision.Version, course.PublishedRevision?.Version, revision.Title, revision.Description,
            revision.Category, revision.Level, revision.Modality, revision.MaxCapacity,
            revision.TotalDurationMinutes, revision.LearningObjectives.ToList(),
            revision.SuggestedPrerequisites.ToList(),
            revision.Modules.OrderBy(x => x.Order).Select(x => new CourseReviewSnapshotModule(
                x.Id, x.Order, x.Name, x.Description, x.DurationMinutes, x.Content, x.Resources.ToList())).ToList(),
            course.CreatedAtUtc, course.UpdatedAtUtc);
        return JsonSerializer.Serialize(payload);
    }

    internal static CourseResponseDTO? Deserialize(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("SchemaVersion", out var version))
            return JsonSerializer.Deserialize<CourseResponseDTO>(json);
        if (version.ValueKind != JsonValueKind.Number || version.GetInt32() != 1)
            throw new CourseStateConflictException("La versión de la instantánea de auditoría no es compatible.");
        var payload = JsonSerializer.Deserialize<CourseReviewSnapshotPayload>(json)
            ?? throw new CourseStateConflictException("La instantánea de auditoría no es válida.");
        return new CourseResponseDTO(payload.Id, payload.CreatedByUserId, payload.OwnerProfessorId,
            payload.Status, payload.WorkingVersion, payload.PublishedVersion, payload.Title,
            payload.Description, payload.Category, payload.Level, payload.Modality,
            payload.MaxCapacity, payload.TotalDurationMinutes, payload.LearningObjectives,
            payload.SuggestedPrerequisites, payload.Modules.Select(x => new CourseModuleResponseDTO(
                x.Id, x.Order, x.Name, x.Description, x.DurationMinutes, x.Content, x.Resources)).ToList(),
            payload.CreatedAtUtc, payload.UpdatedAtUtc) { WorkingStatus = payload.WorkingStatus };
    }
}
