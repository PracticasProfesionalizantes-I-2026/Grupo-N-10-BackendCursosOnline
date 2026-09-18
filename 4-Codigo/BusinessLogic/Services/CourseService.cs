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
        ValidateDraft(dto.MaxCapacity, dto.Modules.Select(x => x.DurationMinutes));
        var now = DateTime.UtcNow;
        var course = new Course
        {
            CreatedByUserId = currentUser.UserId,
            OwnerProfessorId = currentUser.Role == UserRole.Profesor ? currentUser.UserId : null,
            OperationalStatus = CourseOperationalStatus.Activo,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var revision = new CourseRevision
        {
            Version = 1,
            Status = CourseRevisionStatus.Borrador,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        Apply(revision, dto);
        await courseRepository.CreateAsync(course, revision, cancellationToken);
        return MapToResponseDTO(course);
    }

    public async Task<CourseResponseDTO> UpdateAsync(Guid id, CourseUpdateDTO dto, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Role(currentUser, UserRole.Profesor, UserRole.Administrador);
        ValidateDraft(dto.MaxCapacity, dto.Modules.Select(x => x.DurationMinutes));
        var course = await courseRepository.GetByIdForUpdateAsync(id, cancellationToken) ?? throw new CourseNotFoundException(id);
        EnsureCanManage(course);
        var working = course.WorkingRevision ?? throw new CourseStateConflictException("El curso no posee una versión de trabajo.");

        if (working.Status == CourseRevisionStatus.Publicado)
        {
            var nextRevision = CloneWithChanges(course, working, dto);
            await courseRepository.AddRevisionAsync(course, nextRevision, cancellationToken);
        }
        else
        {
            if (working.Status is not (CourseRevisionStatus.Borrador or CourseRevisionStatus.CambiosSolicitados))
            {
                throw new CourseStateConflictException("El estado actual del curso no permite editar su versión de trabajo.");
            }
            ApplyTrackedUpdate(working, dto);
            working.Status = CourseRevisionStatus.Borrador;
            working.UpdatedAtUtc = DateTime.UtcNow;
            course.UpdatedAtUtc = working.UpdatedAtUtc;
            await courseRepository.SaveAsync(cancellationToken);
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
        ValidateComplete(revision);
        revision.Status = CourseRevisionStatus.EnRevision;
        revision.UpdatedAtUtc = DateTime.UtcNow;
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
        if (dto.Decision == CourseReviewDecision.CambiosSolicitados && string.IsNullOrWhiteSpace(dto.Observation))
        {
            throw new LumenValidationException("La observación es obligatoria al solicitar cambios.");
        }
        var review = await courseRepository.GetReviewByIdForUpdateAsync(reviewId, cancellationToken)
            ?? throw new CourseReviewNotFoundException(reviewId);
        var revision = review.CourseRevision;
        if (review.Decision is not null || revision.Status != CourseRevisionStatus.EnRevision)
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
        revision.UpdatedAtUtc = DateTime.UtcNow;
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
        course.UpdatedAtUtc = DateTime.UtcNow;
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
        course.UpdatedAtUtc = DateTime.UtcNow;
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

    private static CourseRevision CloneWithChanges(Course course, CourseRevision source, CourseUpdateDTO dto)
    {
        var now = DateTime.UtcNow;
        var revision = new CourseRevision
        {
            Version = source.Version + 1,
            Status = CourseRevisionStatus.Borrador,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        Apply(revision, dto, source.Modules.Select(x => x.Id).ToHashSet());
        course.UpdatedAtUtc = now;
        return revision;
    }

    private static void Apply(CourseRevision revision, CourseCreateDTO dto)
    {
        revision.Title = dto.Title?.Trim() ?? string.Empty;
        revision.Description = dto.Description?.Trim() ?? string.Empty;
        revision.Category = dto.Category?.Trim() ?? string.Empty;
        revision.Level = dto.Level?.Trim() ?? string.Empty;
        revision.Modality = dto.Modality?.Trim() ?? string.Empty;
        revision.MaxCapacity = dto.MaxCapacity;
        revision.LearningObjectives = dto.LearningObjectives?.Select(x => x.Trim()).Where(x => x.Length > 0).ToList() ?? [];
        revision.SuggestedPrerequisites = dto.SuggestedPrerequisites?.Select(x => x.Trim()).Where(x => x.Length > 0).ToList() ?? [];
        revision.Modules = (dto.Modules ?? []).Select((module, index) => new CourseModule
        {
            Order = index + 1,
            Name = module.Name?.Trim() ?? string.Empty,
            Description = module.Description?.Trim() ?? string.Empty,
            DurationMinutes = module.DurationMinutes,
            Content = module.Content?.Trim() ?? string.Empty,
            Resources = module.Resources?.Select(x => x.Trim()).Where(x => x.Length > 0).ToList() ?? []
        }).ToList();
        revision.TotalDurationMinutes = revision.Modules.Sum(x => x.DurationMinutes);
    }

    private static void Apply(CourseRevision revision, CourseUpdateDTO dto, HashSet<Guid> allowedModuleIds)
    {
        revision.Title = dto.Title?.Trim() ?? string.Empty;
        revision.Description = dto.Description?.Trim() ?? string.Empty;
        revision.Category = dto.Category?.Trim() ?? string.Empty;
        revision.Level = dto.Level?.Trim() ?? string.Empty;
        revision.Modality = dto.Modality?.Trim() ?? string.Empty;
        revision.MaxCapacity = dto.MaxCapacity;
        revision.LearningObjectives = dto.LearningObjectives?.Select(x => x.Trim()).Where(x => x.Length > 0).ToList() ?? [];
        revision.SuggestedPrerequisites = dto.SuggestedPrerequisites?.Select(x => x.Trim()).Where(x => x.Length > 0).ToList() ?? [];
        revision.Modules = (dto.Modules ?? []).Select((module, index) =>
        {
            if (module.Id.HasValue && !allowedModuleIds.Contains(module.Id.Value))
            {
                throw new ModuleCourseMismatchException();
            }
            return new CourseModule
            {
                Id = module.Id ?? Guid.Empty,
                Order = index + 1,
                Name = module.Name?.Trim() ?? string.Empty,
                Description = module.Description?.Trim() ?? string.Empty,
                DurationMinutes = module.DurationMinutes,
                Content = module.Content?.Trim() ?? string.Empty,
                Resources = module.Resources?.Select(x => x.Trim()).Where(x => x.Length > 0).ToList() ?? []
            };
        }).ToList();
        revision.TotalDurationMinutes = revision.Modules.Sum(x => x.DurationMinutes);
    }

    private static void ApplyTrackedUpdate(CourseRevision revision, CourseUpdateDTO dto)
    {
        var existing = revision.Modules.ToDictionary(x => x.Id);
        var suppliedIds = dto.Modules.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToList();
        if (suppliedIds.Count != suppliedIds.Distinct().Count() || suppliedIds.Any(id => !existing.ContainsKey(id)))
        {
            throw new ModuleCourseMismatchException();
        }
        foreach (var omitted in revision.Modules.Where(x => !suppliedIds.Contains(x.Id)).ToList())
        {
            revision.Modules.Remove(omitted);
        }
        var order = 1;
        foreach (var input in dto.Modules)
        {
            CourseModule module;
            if (input.Id.HasValue)
            {
                module = existing[input.Id.Value];
            }
            else
            {
                module = new CourseModule { Id = Guid.NewGuid(), RevisionId = revision.Id };
                revision.Modules.Add(module);
            }
            module.Order = order++;
            module.Name = input.Name?.Trim() ?? string.Empty;
            module.Description = input.Description?.Trim() ?? string.Empty;
            module.DurationMinutes = input.DurationMinutes;
            module.Content = input.Content?.Trim() ?? string.Empty;
            module.Resources = input.Resources?.Select(x => x.Trim()).Where(x => x.Length > 0).ToList() ?? [];
        }
        revision.Title = dto.Title?.Trim() ?? string.Empty;
        revision.Description = dto.Description?.Trim() ?? string.Empty;
        revision.Category = dto.Category?.Trim() ?? string.Empty;
        revision.Level = dto.Level?.Trim() ?? string.Empty;
        revision.Modality = dto.Modality?.Trim() ?? string.Empty;
        revision.MaxCapacity = dto.MaxCapacity;
        revision.LearningObjectives = dto.LearningObjectives?.Select(x => x.Trim()).Where(x => x.Length > 0).ToList() ?? [];
        revision.SuggestedPrerequisites = dto.SuggestedPrerequisites?.Select(x => x.Trim()).Where(x => x.Length > 0).ToList() ?? [];
        revision.TotalDurationMinutes = revision.Modules.Sum(x => x.DurationMinutes);
    }

    private static void ValidateDraft(int maxCapacity, IEnumerable<int> durations)
    {
        if (maxCapacity < 0 || durations.Any(x => x <= 0))
        {
            throw new LumenValidationException("El cupo no puede ser negativo y la duración de cada módulo debe ser mayor que cero.");
        }
    }

    private static void ValidateComplete(CourseRevision revision)
    {
        ServiceGuard.Required(revision.Title, "título");
        ServiceGuard.Required(revision.Description, "descripción");
        ServiceGuard.Required(revision.Category, "categoría");
        ServiceGuard.Required(revision.Level, "nivel");
        ServiceGuard.Required(revision.Modality, "modalidad");
        if (revision.MaxCapacity <= 0)
        {
            throw new LumenValidationException("El cupo máximo debe ser mayor que cero.");
        }
        if (revision.Modules.Count == 0)
        {
            throw new CourseRequiresModuleException();
        }
        foreach (var module in revision.Modules)
        {
            ServiceGuard.Required(module.Name, "nombre del módulo");
            ServiceGuard.Required(module.Description, "descripción del módulo");
            if (module.DurationMinutes <= 0)
            {
                throw new LumenValidationException("La duración de cada módulo debe ser mayor que cero.");
            }
        }
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
