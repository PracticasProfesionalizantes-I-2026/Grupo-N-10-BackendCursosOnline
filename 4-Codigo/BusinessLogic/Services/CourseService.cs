using Lumen.BusinessLogic.Abstractions;
using Lumen.BusinessLogic.Interfaces;
using Lumen.DataAccess.Entities;
using Lumen.DataAccess.Repositories;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Lumen.Shared.Exceptions;

namespace Lumen.BusinessLogic.Services;

public sealed class CourseService(
    ICourseRepository courseRepository,
    ICurrentUser currentUser) : ICourseService
{
    public async Task<IReadOnlyList<PublishedCourseResponseDTO>> GetPublishedAsync(CancellationToken cancellationToken = default)
    {
        ServiceGuard.Authenticated(currentUser);
        var courses = await courseRepository.GetPublishedAsync(cancellationToken);
        return courses.Select(course => MapToPublishedResponseDTO(course, includeContent: false)).ToList();
    }

    public async Task<PublishedCourseResponseDTO> GetPublishedByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Authenticated(currentUser);
        var course = await courseRepository.GetPublishedByIdAsync(id, cancellationToken)
            ?? throw new CourseNotFoundException(id);
        return MapToPublishedResponseDTO(course, includeContent: false);
    }

    public async Task<IReadOnlyList<CourseResponseDTO>> GetManageableAsync(CancellationToken cancellationToken = default)
    {
        ServiceGuard.Role(currentUser, UserRole.Profesor, UserRole.Administrador);
        var courses = currentUser.Role == UserRole.Profesor
            ? await courseRepository.GetByOwnerAsync(currentUser.UserId, cancellationToken)
            : await courseRepository.GetAllAsync(cancellationToken);
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

    public async Task<CourseResponseDTO> UpdateAsync(Guid id, CourseUpdateDTO dto, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Role(currentUser, UserRole.Profesor, UserRole.Administrador);
        var course = await courseRepository.GetByIdForUpdateAsync(id, cancellationToken) ?? throw new CourseNotFoundException(id);
        EnsureCanManage(course);
        var working = course.WorkingRevision ?? throw new CourseStateConflictException("El curso no posee una versión de trabajo.");

        if (working.Status is not (CourseRevisionStatus.Publicado or CourseRevisionStatus.Borrador or CourseRevisionStatus.CambiosSolicitados))
        {
            throw new CourseStateConflictException("El estado actual del curso no permite editar su versión de trabajo.");
        }
        // Validate and build a detached replacement before changing any tracked entity.
        var changes = BuildRevision(dto, working.Modules.Select(x => x.Id).ToHashSet());
        changes.UpdatedAtUtc = NextTimestamp(course.UpdatedAtUtc > working.UpdatedAtUtc ? course.UpdatedAtUtc : working.UpdatedAtUtc);
        if (working.Status == CourseRevisionStatus.Publicado)
        {
            if (working.Version == int.MaxValue)
            {
                throw new CourseStateConflictException("No puede incrementarse la versión del curso.");
            }
            changes.Version = working.Version + 1;
            changes.CreatedAtUtc = changes.UpdatedAtUtc;
            course.UpdatedAtUtc = changes.UpdatedAtUtc;
            await courseRepository.AddRevisionAsync(course, changes, cancellationToken);
        }
        else
        {
            course.UpdatedAtUtc = changes.UpdatedAtUtc;
            await courseRepository.UpdateWorkingRevisionAsync(course, changes, cancellationToken);
        }
        return MapToResponseDTO(course);
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
            RequestedAtUtc = DateTime.UtcNow
        };
        await courseRepository.CreateReviewAsync(review, cancellationToken);
        review.CourseRevision = revision;
        return MapToReviewResponseDTO(review);
    }

    public async Task<IReadOnlyList<CourseReviewResponseDTO>> GetPendingReviewsAsync(CancellationToken cancellationToken = default)
    {
        ServiceGuard.Role(currentUser, UserRole.Administrador);
        var reviews = await courseRepository.GetPendingReviewsAsync(cancellationToken);
        return reviews.Select(MapToReviewResponseDTO).ToList();
    }

    public async Task<IReadOnlyList<CourseReviewResponseDTO>> GetMyReviewsAsync(CancellationToken cancellationToken = default)
    {
        ServiceGuard.Role(currentUser, UserRole.Profesor, UserRole.Administrador);
        var reviews = await courseRepository.GetReviewsByRequesterAsync(currentUser.UserId, cancellationToken);
        return reviews.Select(MapToReviewResponseDTO).ToList();
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
        ChangeOperationalStatusAsync(id, CourseOperationalStatus.Activo, CourseOperationalStatus.Pausado, false, cancellationToken);

    public Task<CourseResponseDTO> ResumeAsync(Guid id, CancellationToken cancellationToken = default) =>
        ChangeOperationalStatusAsync(id, CourseOperationalStatus.Pausado, CourseOperationalStatus.Activo, false, cancellationToken);

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
        await courseRepository.SaveAsync(cancellationToken);
        return MapToResponseDTO(course);
    }

    private async Task<CourseResponseDTO> ChangeOperationalStatusAsync(
        Guid id,
        CourseOperationalStatus expected,
        CourseOperationalStatus target,
        bool adminOnly,
        CancellationToken cancellationToken)
    {
        ServiceGuard.Role(currentUser, adminOnly ? [UserRole.Administrador] : [UserRole.Profesor, UserRole.Administrador]);
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

    private static CourseRevision BuildRevision(CourseUpdateDTO dto, HashSet<Guid> allowedModuleIds)
    {
        if (dto is null || dto.Modules is null || dto.Modules.Any(x => x is null))
        {
            throw new LumenValidationException("El curso y su lista de módulos deben ser válidos.");
        }
        if (dto.MaxCapacity < 0)
        {
            throw new LumenValidationException("El cupo no puede ser negativo.");
        }
        var revision = new CourseRevision
        {
            Status = CourseRevisionStatus.Borrador,
            Title = NormalizeText(dto.Title, 200, "título"),
            Description = NormalizeText(dto.Description, 4000, "descripción"),
            Category = NormalizeText(dto.Category, 100, "categoría"),
            Level = NormalizeText(dto.Level, 100, "nivel"),
            Modality = NormalizeText(dto.Modality, 100, "modalidad"),
            MaxCapacity = dto.MaxCapacity,
            LearningObjectives = NormalizeList(dto.LearningObjectives),
            SuggestedPrerequisites = NormalizeList(dto.SuggestedPrerequisites)
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
                Resources = NormalizeList(input.Resources)
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

    private static List<string> NormalizeList(IReadOnlyList<string>? values)
    {
        if (values is null) return [];
        if (values.Any(x => x is null))
        {
            throw new LumenValidationException("Las listas no pueden contener elementos nulos.");
        }
        return values.Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
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

    private static int ValidateComplete(CourseRevision revision)
    {
        var validated = BuildRevision(new CourseUpdateDTO(revision.Title, revision.Description, revision.Category,
            revision.Level, revision.Modality, revision.MaxCapacity, revision.LearningObjectives,
            revision.SuggestedPrerequisites, revision.Modules.Select(x => new CourseModuleUpdateDTO(x.Id,
                x.Name, x.Description, x.DurationMinutes, x.Content, x.Resources)).ToList()),
            revision.Modules.Select(x => x.Id).ToHashSet());
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
            course.UpdatedAtUtc);
    }

    internal static PublishedCourseResponseDTO MapToPublishedResponseDTO(Course course, bool includeContent)
    {
        var revision = course.PublishedRevision ?? throw new CourseStateConflictException("El curso no posee una versión publicada.");
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

    private static CourseModuleResponseDTO MapToModuleResponseDTO(CourseModule module, bool includeContent) =>
        new(module.Id, module.Order, module.Name, module.Description, module.DurationMinutes,
            includeContent ? module.Content : null,
            includeContent ? module.Resources : []);

    private static CourseReviewResponseDTO MapToReviewResponseDTO(CourseReview review) =>
        new(review.Id, review.CourseRevision.CourseId, review.CourseRevisionId, review.CourseRevision.Version,
            review.Type, review.Decision, review.Observation, review.RequestedByUserId, review.ReviewedByUserId,
            review.RequestedAtUtc, review.ReviewedAtUtc);
}
