using Lumen.DataAccess.Entities;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Lumen.Shared.Exceptions;

namespace Lumen.BusinessLogic.Services;

internal static class CourseValidator
{
    internal static CourseRevision BuildRevision(CourseUpdateDTO dto, HashSet<Guid> allowedModuleIds,
        CourseRevision? baseline = null)
    {
        if (dto is null || dto.Modules is null || dto.Modules.Any(x => x is null))
        {
            throw new LumenValidationException("El curso y su lista de módulos deben ser válidos.");
        }
        if (dto.MaxCapacity <= 0)
        {
            throw new LumenValidationException("El cupo máximo debe ser mayor que cero.");
        }
        ValidateCount(dto.Modules.Count, 100, baseline?.Modules.Count, "módulos");
        var revision = new CourseRevision
        {
            Status = CourseRevisionStatus.Borrador,
            Title = NormalizeText(dto.Title, 200, "título"),
            Description = NormalizeText(dto.Description, 4000, "descripción"),
            Category = NormalizeText(dto.Category, 100, "categoría"),
            Level = NormalizeText(dto.Level, 100, "nivel"),
            Modality = NormalizeText(dto.Modality, 100, "modalidad"),
            MaxCapacity = dto.MaxCapacity,
            LearningObjectives = NormalizeList(dto.LearningObjectives, 30, 500, "objetivos de aprendizaje", baseline?.LearningObjectives),
            SuggestedPrerequisites = NormalizeList(dto.SuggestedPrerequisites, 30, 500, "requisitos previos", baseline?.SuggestedPrerequisites)
        };
        var suppliedIds = new HashSet<Guid>();
        long totalDuration = 0;
        foreach (var input in dto.Modules)
        {
            if (input.Id.HasValue && (!allowedModuleIds.Contains(input.Id.Value) || !suppliedIds.Add(input.Id.Value)))
            {
                throw new ModuleCourseMismatchException();
            }
            if (input.DurationMinutes <= 0)
            {
                throw new LumenValidationException("La duración de cada módulo debe ser mayor que cero.");
            }
            totalDuration += input.DurationMinutes;
            if (totalDuration > int.MaxValue)
            {
                throw new LumenValidationException("La duración total del curso excede el máximo admitido.");
            }
            revision.Modules.Add(new CourseModule
            {
                Id = input.Id ?? Guid.Empty,
                Order = revision.Modules.Count + 1,
                Name = NormalizeText(input.Name, 200, "nombre del módulo"),
                Description = NormalizeText(input.Description, 2000, "descripción del módulo"),
                DurationMinutes = input.DurationMinutes,
                Content = NormalizeText(input.Content, 8000, "contenido del módulo"),
                Resources = NormalizeList(input.Resources, 50, 2000, "recursos", baseline?.Modules.FirstOrDefault(x => x.Id == input.Id)?.Resources)
            });
        }
        revision.TotalDurationMinutes = (int)totalDuration;
        return revision;
    }

    private static string NormalizeText(string? value, int maximumLength, string field)
    {
        ValidateLength(value, maximumLength, field);
        return value?.Trim() ?? string.Empty;
    }

    internal static void ValidateLength(string? value, int maximumLength, string field)
    {
        if (value?.Length > maximumLength)
        {
            throw new LumenValidationException($"El campo {field} admite hasta {maximumLength} caracteres.");
        }
    }

    private static List<string> NormalizeList(IReadOnlyList<string>? values, int maximumCount,
        int maximumLength, string field, IReadOnlyList<string>? baseline)
    {
        if (values is null) return [];
        if (values.Any(x => x is null))
        {
            throw new LumenValidationException("Las listas no pueden contener elementos nulos.");
        }
        var normalized = values.Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
        ValidateCount(normalized.Count, maximumCount, baseline?.Count, field);
        for (var i = 0; i < normalized.Count; i++)
        {
            var previousLength = baseline is not null && i < baseline.Count ? baseline[i].Length : 0;
            if (normalized[i].Length > maximumLength && normalized[i].Length > previousLength)
                throw new LumenValidationException($"Cada elemento de {field} admite hasta {maximumLength} caracteres.");
        }
        return normalized;
    }

    private static void ValidateCount(int count, int maximum, int? previous, string field)
    {
        if (count > maximum && count > (previous ?? 0))
            throw new LumenValidationException($"El curso admite hasta {maximum} {field}.");
    }

    internal static CourseUpdateDTO ToUpdate(CourseRevision revision) =>
        new(revision.Title, revision.Description, revision.Category,
            revision.Level, revision.Modality, revision.MaxCapacity, revision.LearningObjectives,
            revision.SuggestedPrerequisites, revision.Modules.OrderBy(x => x.Order).Select(x => new CourseModuleUpdateDTO(x.Id,
                x.Name, x.Description, x.DurationMinutes, x.Content, x.Resources)).ToList());

    internal static int ValidateComplete(CourseRevision revision)
    {
        var validated = BuildRevision(ToUpdate(revision), revision.Modules.Select(x => x.Id).ToHashSet(), revision);
        ServiceGuard.Required(validated.Title, "título");
        ServiceGuard.Required(validated.Description, "descripción");
        ServiceGuard.Required(validated.Category, "categoría");
        ServiceGuard.Required(validated.Level, "nivel");
        ServiceGuard.Required(validated.Modality, "modalidad");
        if (validated.MaxCapacity <= 0)
        {
            throw new LumenValidationException("El cupo máximo debe ser mayor que cero.");
        }
        if (validated.Modules.Count == 0)
        {
            throw new CourseRequiresModuleException();
        }
        foreach (var module in validated.Modules)
        {
            ServiceGuard.Required(module.Name, "nombre del módulo");
            ServiceGuard.Required(module.Description, "descripción del módulo");
            if (module.DurationMinutes <= 0)
            {
                throw new LumenValidationException("La duración de cada módulo debe ser mayor que cero.");
            }
            if (string.IsNullOrWhiteSpace(module.Content) &&
                !module.Resources.Any(resource => !string.IsNullOrWhiteSpace(resource)))
            {
                throw new LumenValidationException("Cada módulo debe incluir contenido o al menos un recurso.");
            }
        }
        return validated.TotalDurationMinutes;
    }

}
