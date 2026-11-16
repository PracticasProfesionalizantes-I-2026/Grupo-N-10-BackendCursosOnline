using Lumen.BusinessLogic.Abstractions;
using Lumen.BusinessLogic.Interfaces;
using Lumen.DataAccess.Entities;
using Lumen.DataAccess.Repositories;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Lumen.Shared.Exceptions;

namespace Lumen.BusinessLogic.Services;

public sealed class CourseReviewService(ICourseRepository courseRepository, ICurrentUser currentUser) : ICourseReviewService
{
    public async Task<CourseReviewResponseDTO> SubmitForReviewAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Role(currentUser, UserRole.Profesor, UserRole.Administrador);
        var course = await courseRepository.GetByIdForUpdateAsync(id, cancellationToken) ?? throw new CourseNotFoundException(id);
        CourseRules.EnsureCanManage(course, currentUser);
        var revision = course.WorkingRevision ?? throw new CourseStateConflictException("El curso no posee una versión de trabajo.");
        if (revision.Status is not (CourseRevisionStatus.Borrador or CourseRevisionStatus.CambiosSolicitados))
        {
            throw new CourseStateConflictException("Solo una versión en borrador o con cambios solicitados puede enviarse a revisión.");
        }
        var totalDuration = CourseValidator.ValidateComplete(revision);
        var updatedAt = CourseRules.NextTimestamp(course.UpdatedAtUtc > revision.UpdatedAtUtc ? course.UpdatedAtUtc : revision.UpdatedAtUtc);
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
            Snapshot = new CourseReviewSnapshot { ContentJson = CourseReviewSnapshotCodec.Serialize(course) }
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
            : CourseReviewSnapshotCodec.Deserialize(review.Snapshot.ContentJson));
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
        CourseValidator.ValidateLength(dto.Observation, 2000, "observación");
        if (dto.Decision == CourseReviewDecision.CambiosSolicitados && string.IsNullOrWhiteSpace(dto.Observation))
        {
            throw new LumenValidationException("La observación es obligatoria al solicitar cambios.");
        }
        var review = await courseRepository.GetReviewByIdForUpdateAsync(reviewId, cancellationToken)
            ?? throw new CourseReviewNotFoundException(reviewId);
        if (review.RequestedByUserId == currentUser.UserId)
        {
            throw new ForbiddenOperationException("Un administrador no puede resolver una revisión que solicitó él mismo.");
        }
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
        revision.UpdatedAtUtc = CourseRules.NextTimestamp(revision.Course.UpdatedAtUtc > revision.UpdatedAtUtc ? revision.Course.UpdatedAtUtc : revision.UpdatedAtUtc);
        revision.Course.UpdatedAtUtc = revision.UpdatedAtUtc;
        await courseRepository.SaveAsync(cancellationToken);
        return MapToReviewResponseDTO(review);
    }

    private static CourseReviewResponseDTO MapToReviewResponseDTO(CourseReview review) =>
        new(review.Id, review.CourseRevision.CourseId, review.CourseRevisionId, review.CourseRevision.Version,
            review.Type, review.Decision, review.Observation, review.RequestedByUserId, review.ReviewedByUserId,
            review.RequestedAtUtc, review.ReviewedAtUtc);
}
