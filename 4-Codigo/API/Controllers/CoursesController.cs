using Lumen.API.Http;
using Lumen.BusinessLogic.Interfaces;
using Lumen.Shared.DTOs;
using Lumen.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.API.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class CoursesController(ICourseService service) : ControllerBase
{
    [HttpGet("catalog/courses")]
    [ProducesResponseType<IReadOnlyList<PublishedCourseResponseDTO>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCatalog(int? limit, int? offset, CancellationToken cancellationToken) =>
        Ok(await service.GetPublishedAsync(cancellationToken, ReadPage(limit, offset)));

    [HttpGet("catalog/courses/{id:guid}")]
    [ProducesResponseType<PublishedCourseResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublished(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetPublishedByIdAsync(id, cancellationToken));

    [Authorize(Roles = "Profesor,Administrador")]
    [HttpGet("courses")]
    [ProducesResponseType<IReadOnlyList<CourseResponseDTO>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetManageable(int? limit, int? offset, CancellationToken cancellationToken) =>
        Ok(await service.GetManageableAsync(cancellationToken, ReadPage(limit, offset)));

    [Authorize(Roles = "Profesor,Administrador")]
    [HttpGet("courses/{id:guid}")]
    [ProducesResponseType<CourseResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        WithEditTag(await service.GetByIdAsync(id, cancellationToken));

    [Authorize(Roles = "Profesor,Administrador")]
    [HttpPost("courses")]
    [ProducesResponseType<CourseResponseDTO>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CourseCreateDTO dto, CancellationToken cancellationToken)
    {
        var value = await service.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = value.Id }, value);
    }

    [Authorize(Roles = "Profesor,Administrador")]
    [HttpPut("courses/{id:guid}")]
    [ProducesResponseType<CourseResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status412PreconditionFailed)]
    public async Task<IActionResult> Update(Guid id, CourseUpdateDTO dto, CancellationToken cancellationToken) =>
        WithEditTag(await service.UpdateAsync(id, dto, cancellationToken,
            CourseEditTag.Read(id, Request.Headers.IfMatch.Select(value => value ?? string.Empty).ToArray())));

    [Authorize(Roles = "Profesor,Administrador")]
    [HttpPost("courses/{id:guid}/submit-review")]
    [ProducesResponseType<CourseReviewResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitReview(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.SubmitForReviewAsync(id, cancellationToken));

    [Authorize(Roles = "Profesor,Administrador")]
    [HttpPost("courses/{id:guid}/pause")]
    [ProducesResponseType<CourseResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Pause(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.PauseAsync(id, cancellationToken));

    [Authorize(Roles = "Profesor,Administrador")]
    [HttpPost("courses/{id:guid}/resume")]
    [ProducesResponseType<CourseResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Resume(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.ResumeAsync(id, cancellationToken));

    [Authorize(Roles = "Administrador")]
    [HttpPost("courses/{id:guid}/finish")]
    [ProducesResponseType<CourseResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Finish(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.FinishAsync(id, cancellationToken));

    [Authorize(Roles = "Administrador")]
    [HttpGet("course-reviews")]
    [ProducesResponseType<IReadOnlyList<CourseReviewResponseDTO>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingReviews(CancellationToken cancellationToken) =>
        Ok(await service.GetPendingReviewsAsync(cancellationToken));

    [Authorize(Roles = "Profesor,Administrador")]
    [HttpGet("course-reviews/mine")]
    [ProducesResponseType<IReadOnlyList<CourseReviewResponseDTO>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyReviews(CancellationToken cancellationToken) =>
        Ok(await service.GetMyReviewsAsync(cancellationToken));

    [Authorize(Roles = "Administrador")]
    [HttpPost("course-reviews/{id:guid}/decision")]
    [ProducesResponseType<CourseReviewResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResolveReview(Guid id, CourseReviewDecisionDTO dto, CancellationToken cancellationToken) =>
        Ok(await service.ResolveReviewAsync(id, dto, cancellationToken));

    private IActionResult WithEditTag(CourseResponseDTO course)
    {
        Response.Headers.ETag = CourseEditTag.For(course);
        Response.Headers.CacheControl = "private, no-store";
        return Ok(course);
    }

    private static CoursePage? ReadPage(int? limit, int? offset)
    {
        if (limit is null && offset is null) return null;
        if (limit is null or < 1 or > 100 || offset is < 0)
        {
            throw new LumenValidationException("La paginación requiere limit entre 1 y 100 y offset mayor o igual a cero.");
        }
        return new CoursePage(offset ?? 0, limit.Value);
    }
}
