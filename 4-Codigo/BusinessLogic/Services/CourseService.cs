using Lumen.BusinessLogic.Abstractions;
using Lumen.BusinessLogic.Interfaces;
using Lumen.DataAccess.Entities;
using Lumen.DataAccess.Repositories;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Lumen.Shared.Exceptions;
using System.Text.Json;

namespace Lumen.BusinessLogic.Services;

public sealed class CourseService(
    ICourseRepository courseRepository,
    ICurrentUser currentUser) : ICourseService
{
    public async Task<IReadOnlyList<PublishedCourseResponseDTO>> GetPublishedAsync(CancellationToken cancellationToken = default, ListPage? page = null)
    {
        ServiceGuard.Authenticated(currentUser);
        var courses = await courseRepository.GetPublishedAsync(cancellationToken, page);
        return courses.Select(course => MapToPublishedResponseDTO(course, includeContent: false)).ToList();
    }

    public async Task<PublishedCourseResponseDTO> GetPublishedByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Authenticated(currentUser);
        var course = await courseRepository.GetPublishedByIdAsync(id, cancellationToken)
            ?? throw new CourseNotFoundException(id);
        return MapToPublishedResponseDTO(course, includeContent: false);
    }

    public async Task<IReadOnlyList<CourseResponseDTO>> GetManageableAsync(CancellationToken cancellationToken = default, ListPage? page = null)
    {
        ServiceGuard.Role(currentUser, UserRole.Profesor, UserRole.Administrador);
        var courses = currentUser.Role == UserRole.Profesor
            ? await courseRepository.GetByOwnerAsync(currentUser.UserId, cancellationToken, page)
            : await courseRepository.GetAllAsync(cancellationToken, page);
        return courses.Select(MapToResponseDTO).ToList();
    }

    public async Task<CourseResponseDTO> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Role(currentUser, UserRole.Profesor, UserRole.Administrador);
        var course = await courseRepository.GetByIdAsync(id, cancellationToken) ?? throw new CourseNotFoundException(id);
        EnsureCanManage(course);
        return MapToResponseDTO(course);
    }

    public async Task<CourseResponseDTO> CreateAsync(CourseCreateDTO dto, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Role(currentUser, UserRole.Profesor, UserRole.Administrador);
        if (dto is null || dto.Modules is null || dto.Modules.Any(x => x is null))
        {
            throw new LumenValidationException("El curso y su lista de módulos deben ser válidos.");
        }
        var revision = BuildRevision(new CourseUpdateDTO(dto.Title, dto.Description, dto.Category, dto.Level,
            dto.Modality, dto.MaxCapacity, dto.LearningObjectives, dto.SuggestedPrerequisites,
            dto.Modules.Select(x => new CourseModuleUpdateDTO(null, x.Name, x.Description, x.DurationMinutes,
                x.Content, x.Resources)).ToList()), []);
        var now = DateTime.UtcNow;
        var course = new Course
        {
            CreatedByUserId = currentUser.UserId,
            OwnerProfessorId = currentUser.Role == UserRole.Profesor ? currentUser.UserId : null,
            OperationalStatus = CourseOperationalStatus.Activo,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        revision.Version = 1;
        revision.CreatedAtUtc = now;
        revision.UpdatedAtUtc = now;
        await courseRepository.CreateAsync(course, revision, cancellationToken);
        return MapToResponseDTO(course);
    }

    public async Task<CourseResponseDTO> UpdateAsync(Guid id, CourseUpdateDTO dto, CancellationToken cancellationToken = default,
        IReadOnlyCollection<long>? expectedUpdates = null)
    {
        ServiceGuard.Role(currentUser, UserRole.Profesor, UserRole.Administrador);
        var course = await courseRepository.GetByIdForUpdateAsync(id, cancellationToken) ?? throw new CourseNotFoundException(id);
        EnsureCanManage(course);
        EnsureExpectedVersion(course, expectedUpdates);
        var working = course.WorkingRevision ?? throw new CourseStateConflictException("El curso no posee una versión de trabajo.");

        if (working.Status is not (CourseRevisionStatus.Publicado or CourseRevisionStatus.Borrador or CourseRevisionStatus.CambiosSolicitados))
        {
            throw new CourseStateConflictException("El estado actual del curso no permite editar su versión de trabajo.");
        }
        // Validate and build a detached replacement before changing any tracked entity.
        var changes = BuildRevision(dto, working.Modules.Select(x => x.Id).ToHashSet(), working);
        if (changes.MaxCapacity < working.MaxCapacity &&
            changes.MaxCapacity < await courseRepository.GetApprovedCountAsync(id, cancellationToken))
            throw new CourseStateConflictException("El cupo no puede reducirse por debajo de las inscripciones aprobadas.");
        KeepOnlyUnchangedPublishedModuleIds(course, changes);
        if (working.Status == CourseRevisionStatus.Publicado)
        {
            await AddWorkingRevisionAsync(course, working, changes, expectedUpdates, cancellationToken);
        }
        else
        {
            changes.UpdatedAtUtc = NextTimestamp(course.UpdatedAtUtc > working.UpdatedAtUtc ? course.UpdatedAtUtc : working.UpdatedAtUtc);
            course.UpdatedAtUtc = changes.UpdatedAtUtc;
            await SaveEditAsync(() => courseRepository.UpdateWorkingRevisionAsync(course, changes, cancellationToken), expectedUpdates);
        }
        return MapToResponseDTO(course);
    }

    public async Task<CourseResponseDTO> RecoverRejectedDraftAsync(Guid id, IReadOnlyCollection<long> expectedUpdates,
        CancellationToken cancellationToken = default)
    {
        ServiceGuard.Role(currentUser, UserRole.Profesor, UserRole.Administrador);
        var course = await courseRepository.GetByIdForUpdateAsync(id, cancellationToken) ?? throw new CourseNotFoundException(id);
        EnsureCanManage(course);
        EnsureExpectedVersion(course, expectedUpdates);
        if (course.WorkingRevision is not { Status: CourseRevisionStatus.Rechazado } rejected)
            throw new CourseStateConflictException("Solo se puede recuperar una revisión de trabajo rechazada.");

        var draft = BuildRevision(ToUpdate(rejected), rejected.Modules.Select(x => x.Id).ToHashSet(), rejected);
        KeepOnlyUnchangedPublishedModuleIds(course, draft);
        await AddWorkingRevisionAsync(course, rejected, draft, expectedUpdates, cancellationToken);
        return MapToResponseDTO(course);
    }

    private async Task AddWorkingRevisionAsync(Course course, CourseRevision previous, CourseRevision changes,
        IReadOnlyCollection<long>? expectedUpdates, CancellationToken cancellationToken)
    {
        if (previous.Version == int.MaxValue)
            throw new CourseStateConflictException("No puede incrementarse la versión del curso.");

        changes.Version = previous.Version + 1;
        changes.UpdatedAtUtc = NextTimestamp(course.UpdatedAtUtc > previous.UpdatedAtUtc ? course.UpdatedAtUtc : previous.UpdatedAtUtc);
        changes.CreatedAtUtc = changes.UpdatedAtUtc;
        course.UpdatedAtUtc = changes.UpdatedAtUtc;
        await SaveEditAsync(() => courseRepository.AddRevisionAsync(course, changes, cancellationToken), expectedUpdates);
    }

    private static void EnsureExpectedVersion(Course course, IReadOnlyCollection<long>? expectedUpdates)
    {
        if (expectedUpdates is not null && !expectedUpdates.Contains(course.UpdatedAtUtc.Ticks))
            throw new CourseVersionMismatchException();
    }

    private static async Task SaveEditAsync(Func<Task> save, IReadOnlyCollection<long>? expectedUpdates)
    {
        try
        {
            await save();
        }
        catch (CourseStateConflictException) when (expectedUpdates is not null)
        {
            throw new CourseVersionMismatchException();
        }
    }

    public async Task<CourseReviewResponseDTO> SubmitForReviewAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Role(currentUser, UserRole.Profesor, UserRole.Administrador);
        var course = await courseRepository.GetByIdForUpdateAsync(id, cancellationToken) ?? throw new CourseNotFoundException(id);
        EnsureCanManage(course);
        var revision = course.WorkingRevision ?? throw new CourseStateConflictException("El curso no posee una versión de trabajo.");
        if (revision.Status is not (CourseRevisionStatus.Borrador or CourseRevisionStatus.CambiosSolicitados))
        {
            throw new CourseStateConflictException("Solo una versión en borrador o con cambios solicitados puede enviarse a revisión.");
        }
        var totalDuration = ValidateComplete(revision);
        var updatedAt = NextTimestamp(course.UpdatedAtUtc > revision.UpdatedAtUtc ? course.UpdatedAtUtc : revision.UpdatedAtUtc);
        revision.Status = CourseRevisionStatus.EnRevision;
        revision.TotalDurationMinutes = totalDuration;
        revision.UpdatedAtUtc = updatedAt;
        course.UpdatedAtUtc = revision.UpdatedAtUtc;
        var review = new CourseReview
        {
            CourseRevisionId = revision.Id,
            Type = course.PublishedRevisionId is null ? CourseReviewType.Creacion : CourseReviewType.Modificacion,
            RequestedByUserId = currentUser.UserId,
            RequestedAtUtc = updatedAt,
            Snapshot = new CourseReviewSnapshot { ContentJson = JsonSerializer.Serialize(MapToResponseDTO(course)) }
        };
        await courseRepository.CreateReviewAsync(review, cancellationToken);
        review.CourseRevision = revision;
        return MapToReviewResponseDTO(review);
    }

    public async Task<IReadOnlyList<CourseReviewResponseDTO>> GetPendingReviewsAsync(CancellationToken cancellationToken = default, ListPage? page = null)
    {
        ServiceGuard.Role(currentUser, UserRole.Administrador);
        var reviews = await courseRepository.GetPendingReviewsAsync(cancellationToken, page);
        return reviews.Select(MapToReviewResponseDTO).ToList();
    }

    public async Task<IReadOnlyList<CourseReviewResponseDTO>> GetMyReviewsAsync(CancellationToken cancellationToken = default, ListPage? page = null)
    {
        ServiceGuard.Role(currentUser, UserRole.Profesor, UserRole.Administrador);
        var reviews = await courseRepository.GetReviewsByRequesterAsync(currentUser.UserId, cancellationToken, page);
        return reviews.Select(MapToReviewResponseDTO).ToList();
    }

    public async Task<CourseReviewSnapshotResponseDTO> GetReviewSnapshotAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Role(currentUser, UserRole.Profesor, UserRole.Administrador);
        var review = await courseRepository.GetReviewByIdAsync(id, cancellationToken) ?? throw new CourseReviewNotFoundException(id);
        if (currentUser.Role != UserRole.Administrador && review.RequestedByUserId != currentUser.UserId)
            throw new ForbiddenOperationException();
        return new CourseReviewSnapshotResponseDTO(MapToReviewResponseDTO(review), review.Snapshot is null
            ? null
            : JsonSerializer.Deserialize<CourseResponseDTO>(review.Snapshot.ContentJson));
    }

    public async Task<CourseReviewResponseDTO> ResolveReviewAsync(
        Guid reviewId,
        CourseReviewDecisionDTO dto,
        CancellationToken cancellationToken = default)
    {
        ServiceGuard.Role(currentUser, UserRole.Administrador);
        if (dto is null || !Enum.IsDefined(dto.Decision))
        {
            throw new LumenValidationException("La decisión de auditoría no es válida.");
        }
        ValidateLength(dto.Observation, 2000, "observación");
        if (dto.Decision == CourseReviewDecision.CambiosSolicitados && string.IsNullOrWhiteSpace(dto.Observation))
        {
            throw new LumenValidationException("La observación es obligatoria al solicitar cambios.");
        }
        var review = await courseRepository.GetReviewByIdForUpdateAsync(reviewId, cancellationToken)
            ?? throw new CourseReviewNotFoundException(reviewId);
        var revision = review.CourseRevision;
        if (review.Decision is not null || revision.Status != CourseRevisionStatus.EnRevision ||
            revision.Course.WorkingRevisionId != revision.Id)
        {
            throw new CourseStateConflictException("La solicitud ya fue resuelta o no corresponde a una versión en revisión.");
        }

        review.Decision = dto.Decision;
        review.Observation = string.IsNullOrWhiteSpace(dto.Observation) ? null : dto.Observation.Trim();
        review.ReviewedByUserId = currentUser.UserId;
        review.ReviewedAtUtc = DateTime.UtcNow;
        switch (dto.Decision)
        {
            case CourseReviewDecision.Aprobada:
                revision.Status = CourseRevisionStatus.Publicado;
                revision.PublishedAtUtc = DateTime.UtcNow;
                revision.Course.PublishedRevisionId = revision.Id;
                break;
            case CourseReviewDecision.CambiosSolicitados:
                revision.Status = CourseRevisionStatus.CambiosSolicitados;
                break;
            case CourseReviewDecision.Rechazada:
                revision.Status = CourseRevisionStatus.Rechazado;
                break;
            default:
                throw new LumenValidationException("La decisión de auditoría no es válida.");
        }
        revision.UpdatedAtUtc = NextTimestamp(revision.Course.UpdatedAtUtc > revision.UpdatedAtUtc ? revision.Course.UpdatedAtUtc : revision.UpdatedAtUtc);
        revision.Course.UpdatedAtUtc = revision.UpdatedAtUtc;
        await courseRepository.SaveAsync(cancellationToken);
        return MapToReviewResponseDTO(review);
    }

    public Task<CourseResponseDTO> PauseAsync(Guid id, CancellationToken cancellationToken = default) =>
        ChangeOperationalStatusAsync(id, CourseOperationalStatus.Activo, CourseOperationalStatus.Pausado, cancellationToken);

    public Task<CourseResponseDTO> ResumeAsync(Guid id, CancellationToken cancellationToken = default) =>
        ChangeOperationalStatusAsync(id, CourseOperationalStatus.Pausado, CourseOperationalStatus.Activo, cancellationToken);

    public async Task<CourseResponseDTO> FinishAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Role(currentUser, UserRole.Administrador);
        var course = await courseRepository.GetByIdForUpdateAsync(id, cancellationToken) ?? throw new CourseNotFoundException(id);
        if (course.PublishedRevisionId is null ||
            course.OperationalStatus is not (CourseOperationalStatus.Activo or CourseOperationalStatus.Pausado))
        {
            throw new CourseStateConflictException("Solo puede finalizarse un curso publicado o pausado.");
        }
        course.OperationalStatus = CourseOperationalStatus.Finalizado;
        course.UpdatedAtUtc = NextTimestamp(course.UpdatedAtUtc);
        await courseRepository.FinishAndRejectPendingAsync(course, currentUser.UserId, DateTime.UtcNow, cancellationToken);
        return MapToResponseDTO(course);
    }

    private async Task<CourseResponseDTO> ChangeOperationalStatusAsync(
        Guid id,
        CourseOperationalStatus expected,
        CourseOperationalStatus target,
        CancellationToken cancellationToken)
    {
        ServiceGuard.Role(currentUser, UserRole.Profesor, UserRole.Administrador);
        var course = await courseRepository.GetByIdForUpdateAsync(id, cancellationToken) ?? throw new CourseNotFoundException(id);
        EnsureCanManage(course);
        if (course.PublishedRevisionId is null || course.OperationalStatus != expected)
        {
            throw new CourseStateConflictException($"El estado actual del curso no permite cambiarlo a {target}.");
        }
        course.OperationalStatus = target;
        course.UpdatedAtUtc = NextTimestamp(course.UpdatedAtUtc);
        await courseRepository.SaveAsync(cancellationToken);
        return MapToResponseDTO(course);
    }

    private void EnsureCanManage(Course course)
    {
        if (currentUser.Role == UserRole.Profesor && course.OwnerProfessorId != currentUser.UserId)
        {
            throw new CourseOwnershipException();
        }
    }

    private static CourseRevision BuildRevision(CourseUpdateDTO dto, HashSet<Guid> allowedModuleIds,
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

    private static void ValidateLength(string? value, int maximumLength, string field)
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

    private static DateTime NextTimestamp(DateTime previous)
    {
        var now = DateTime.UtcNow;
        if (now > previous) return now;
        if (previous == DateTime.MaxValue)
        {
            throw new CourseStateConflictException("La fecha de actualización del curso no permite registrar cambios.");
        }
        return previous.AddTicks(1);
    }

    private static CourseUpdateDTO ToUpdate(CourseRevision revision) =>
        new(revision.Title, revision.Description, revision.Category,
            revision.Level, revision.Modality, revision.MaxCapacity, revision.LearningObjectives,
            revision.SuggestedPrerequisites, revision.Modules.OrderBy(x => x.Order).Select(x => new CourseModuleUpdateDTO(x.Id,
                x.Name, x.Description, x.DurationMinutes, x.Content, x.Resources)).ToList());

    private static void KeepOnlyUnchangedPublishedModuleIds(Course course, CourseRevision changes)
    {
        if (course.PublishedRevision is not { } published) return;
        var originalById = published.Modules.ToDictionary(x => x.Id);
        foreach (var module in changes.Modules)
        {
            if (originalById.TryGetValue(module.Id, out var original) &&
                (module.Name != original.Name || module.Description != original.Description ||
                 module.DurationMinutes != original.DurationMinutes || module.Content != original.Content ||
                 !module.Resources.SequenceEqual(original.Resources)))
            {
                // A completion follows an unchanged module, never a replacement with different content.
                module.Id = Guid.Empty;
            }
        }
    }

    private static int ValidateComplete(CourseRevision revision)
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

    private static CourseResponseDTO MapToResponseDTO(Course course)
    {
        var revision = course.WorkingRevision ?? throw new CourseStateConflictException("El curso no posee una versión de trabajo.");
        var status = course.OperationalStatus switch
        {
            CourseOperationalStatus.Pausado => "Pausado",
            CourseOperationalStatus.Finalizado => "Finalizado",
            _ => revision.Status.ToString()
        };
        return new CourseResponseDTO(
            course.Id,
            course.CreatedByUserId,
            course.OwnerProfessorId,
            status,
            revision.Version,
            course.PublishedRevision?.Version,
            revision.Title,
            revision.Description,
            revision.Category,
            revision.Level,
            revision.Modality,
            revision.MaxCapacity,
            revision.TotalDurationMinutes,
            revision.LearningObjectives,
            revision.SuggestedPrerequisites,
            revision.Modules.OrderBy(x => x.Order).Select(x => MapToModuleResponseDTO(x, true)).ToList(),
            course.CreatedAtUtc,
            course.UpdatedAtUtc) { WorkingStatus = revision.Status.ToString() };
    }

    internal static PublishedCourseResponseDTO MapToPublishedResponseDTO(Course course, bool includeContent)
    {
        var revision = RequireOwnPublication(course);
        return new PublishedCourseResponseDTO(
            course.Id,
            revision.Version,
            revision.Title,
            revision.Description,
            revision.Category,
            revision.Level,
            revision.Modality,
            revision.MaxCapacity,
            revision.TotalDurationMinutes,
            revision.LearningObjectives,
            revision.SuggestedPrerequisites,
            revision.Modules.OrderBy(x => x.Order).Select(x => MapToModuleResponseDTO(x, includeContent)).ToList(),
            revision.PublishedAtUtc ?? revision.UpdatedAtUtc);
    }

    internal static CourseRevision RequireOwnPublication(Course course) =>
        OwnRevision(course, course.PublishedRevision)
            ?? throw new CourseStateConflictException("El curso no posee una versión publicada válida.");

    internal static CourseRevision? OwnRevision(Course course, CourseRevision? revision) =>
        revision?.CourseId == course.Id ? revision : null;

    private static CourseModuleResponseDTO MapToModuleResponseDTO(CourseModule module, bool includeContent) =>
        new(module.Id, module.Order, module.Name, module.Description, module.DurationMinutes,
            includeContent ? module.Content : null,
            includeContent ? module.Resources : []);

    private static CourseReviewResponseDTO MapToReviewResponseDTO(CourseReview review) =>
        new(review.Id, review.CourseRevision.CourseId, review.CourseRevisionId, review.CourseRevision.Version,
            review.Type, review.Decision, review.Observation, review.RequestedByUserId, review.ReviewedByUserId,
            review.RequestedAtUtc, review.ReviewedAtUtc);
}
