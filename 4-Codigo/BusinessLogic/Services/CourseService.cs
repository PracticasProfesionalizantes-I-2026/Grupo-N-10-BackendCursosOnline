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
        CourseRules.EnsureCanManage(course, currentUser);
        return MapToResponseDTO(course);
    }

    public async Task<CourseResponseDTO> CreateAsync(CourseCreateDTO dto, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Role(currentUser, UserRole.Profesor, UserRole.Administrador);
        if (dto is null || dto.Modules is null || dto.Modules.Any(x => x is null))
        {
            throw new LumenValidationException("El curso y su lista de módulos deben ser válidos.");
        }
        var revision = CourseValidator.BuildRevision(new CourseUpdateDTO(dto.Title, dto.Description, dto.Category, dto.Level,
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
        CourseRules.EnsureCanManage(course, currentUser);
        EnsureExpectedVersion(course, expectedUpdates);
        var working = course.WorkingRevision ?? throw new CourseStateConflictException("El curso no posee una versión de trabajo.");

        if (working.Status is not (CourseRevisionStatus.Publicado or CourseRevisionStatus.Borrador or CourseRevisionStatus.CambiosSolicitados))
        {
            throw new CourseStateConflictException("El estado actual del curso no permite editar su versión de trabajo.");
        }
        // Validate and build a detached replacement before changing any tracked entity.
        var changes = CourseValidator.BuildRevision(dto, working.Modules.Select(x => x.Id).ToHashSet(), working);
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
            changes.UpdatedAtUtc = CourseRules.NextTimestamp(course.UpdatedAtUtc > working.UpdatedAtUtc ? course.UpdatedAtUtc : working.UpdatedAtUtc);
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
        CourseRules.EnsureCanManage(course, currentUser);
        EnsureExpectedVersion(course, expectedUpdates);
        if (course.WorkingRevision is not { Status: CourseRevisionStatus.Rechazado } rejected)
            throw new CourseStateConflictException("Solo se puede recuperar una revisión de trabajo rechazada.");

        var draft = CourseValidator.BuildRevision(CourseValidator.ToUpdate(rejected), rejected.Modules.Select(x => x.Id).ToHashSet(), rejected);
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
        changes.UpdatedAtUtc = CourseRules.NextTimestamp(course.UpdatedAtUtc > previous.UpdatedAtUtc ? course.UpdatedAtUtc : previous.UpdatedAtUtc);
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
        course.UpdatedAtUtc = CourseRules.NextTimestamp(course.UpdatedAtUtc);
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
        CourseRules.EnsureCanManage(course, currentUser);
        if (course.PublishedRevisionId is null || course.OperationalStatus != expected)
        {
            throw new CourseStateConflictException($"El estado actual del curso no permite cambiarlo a {target}.");
        }
        course.OperationalStatus = target;
        course.UpdatedAtUtc = CourseRules.NextTimestamp(course.UpdatedAtUtc);
        await courseRepository.SaveAsync(cancellationToken);
        return MapToResponseDTO(course);
    }

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


}
