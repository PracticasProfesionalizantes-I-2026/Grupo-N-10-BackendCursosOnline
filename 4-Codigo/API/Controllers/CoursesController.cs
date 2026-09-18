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
    public Task<IActionResult> GetCatalog(CancellationToken cancellationToken) =>
        ExecuteAsync(() => service.GetPublishedAsync(cancellationToken), Ok);

    [HttpGet("catalog/courses/{id:guid}")]
    public Task<IActionResult> GetPublished(Guid id, CancellationToken cancellationToken) =>
        ExecuteAsync(() => service.GetPublishedByIdAsync(id, cancellationToken), Ok);

    [Authorize(Roles = "Profesor,Administrador")]
    [HttpGet("courses")]
    public Task<IActionResult> GetManageable(CancellationToken cancellationToken) =>
        ExecuteAsync(() => service.GetManageableAsync(cancellationToken), Ok);

    [Authorize(Roles = "Profesor,Administrador")]
    [HttpGet("courses/{id:guid}")]
    public Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        ExecuteAsync(() => service.GetByIdAsync(id, cancellationToken), Ok);

    [Authorize(Roles = "Profesor,Administrador")]
    [HttpPost("courses")]
    public Task<IActionResult> Create(CourseCreateDTO dto, CancellationToken cancellationToken) =>
        ExecuteAsync(() => service.CreateAsync(dto, cancellationToken),
            value => CreatedAtAction(nameof(GetById), new { id = value.Id }, value));

    [Authorize(Roles = "Profesor,Administrador")]
    [HttpPut("courses/{id:guid}")]
    public Task<IActionResult> Update(Guid id, CourseUpdateDTO dto, CancellationToken cancellationToken) =>
        ExecuteAsync(() => service.UpdateAsync(id, dto, cancellationToken), Ok);

    [Authorize(Roles = "Profesor,Administrador")]
    [HttpPost("courses/{id:guid}/submit-review")]
    public Task<IActionResult> SubmitReview(Guid id, CancellationToken cancellationToken) =>
        ExecuteAsync(() => service.SubmitForReviewAsync(id, cancellationToken), Ok);

    [Authorize(Roles = "Profesor,Administrador")]
    [HttpPost("courses/{id:guid}/pause")]
    public Task<IActionResult> Pause(Guid id, CancellationToken cancellationToken) =>
        ExecuteAsync(() => service.PauseAsync(id, cancellationToken), Ok);

    [Authorize(Roles = "Profesor,Administrador")]
    [HttpPost("courses/{id:guid}/resume")]
    public Task<IActionResult> Resume(Guid id, CancellationToken cancellationToken) =>
        ExecuteAsync(() => service.ResumeAsync(id, cancellationToken), Ok);

    [Authorize(Roles = "Administrador")]
    [HttpPost("courses/{id:guid}/finish")]
    public Task<IActionResult> Finish(Guid id, CancellationToken cancellationToken) =>
        ExecuteAsync(() => service.FinishAsync(id, cancellationToken), Ok);

    [Authorize(Roles = "Administrador")]
    [HttpGet("course-reviews")]
    public Task<IActionResult> GetPendingReviews(CancellationToken cancellationToken) =>
        ExecuteAsync(() => service.GetPendingReviewsAsync(cancellationToken), Ok);

    [Authorize(Roles = "Profesor,Administrador")]
    [HttpGet("course-reviews/mine")]
    public Task<IActionResult> GetMyReviews(CancellationToken cancellationToken) =>
        ExecuteAsync(() => service.GetMyReviewsAsync(cancellationToken), Ok);

    [Authorize(Roles = "Administrador")]
    [HttpPost("course-reviews/{id:guid}/decision")]
    public Task<IActionResult> ResolveReview(Guid id, CourseReviewDecisionDTO dto, CancellationToken cancellationToken) =>
        ExecuteAsync(() => service.ResolveReviewAsync(id, dto, cancellationToken), Ok);

    private async Task<IActionResult> ExecuteAsync<T>(Func<Task<T>> operation, Func<T, IActionResult> success)
    {
        try { return success(await operation()); }
        catch (Exception ex) when (ex is LumenValidationException or PublicRegistrationRoleNotAllowedException or CourseRequiresModuleException or InvalidDateRangeException)
        { return BadRequest(new ApiErrorResponseDTO(ex.GetType().Name, ex.Message)); }
        catch (InvalidCredentialsException ex)
        { return Unauthorized(new ApiErrorResponseDTO(ex.GetType().Name, ex.Message)); }
        catch (Exception ex) when (ex is InactiveUserException or ForbiddenOperationException or CourseOwnershipException or EnrollmentOwnershipException)
        { return StatusCode(StatusCodes.Status403Forbidden, new ApiErrorResponseDTO(ex.GetType().Name, ex.Message)); }
        catch (Exception ex) when (ex is UserNotFoundException or CourseNotFoundException or EnrollmentNotFoundException or ModuleNotFoundException or CourseReviewNotFoundException)
        { return NotFound(new ApiErrorResponseDTO(ex.GetType().Name, ex.Message)); }
        catch (Exception ex) when (ex is DuplicateEmailException or DuplicateEnrollmentException or CourseStateConflictException or EnrollmentStateConflictException or ModuleCourseMismatchException or DependencyConflictException)
        { return Conflict(new ApiErrorResponseDTO(ex.GetType().Name, ex.Message)); }
    }
}

