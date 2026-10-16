using Lumen.BusinessLogic.Interfaces;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Lumen.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.API.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class EnrollmentsController(
    IEnrollmentService enrollmentService,
    IProgressService progressService) : ControllerBase
{
    [Authorize(Roles = "Alumno")]
    [HttpPost("enrollments")]
    [ProducesResponseType<EnrollmentResponseDTO>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create(EnrollmentCreateDTO dto, CancellationToken cancellationToken) =>
        ExecuteAsync(() => enrollmentService.CreateAsync(dto, cancellationToken),
            value => CreatedAtAction(nameof(GetById), new { id = value.Id }, value));

    [Authorize(Roles = "Alumno")]
    [HttpGet("enrollments/mine")]
    [ProducesResponseType<IReadOnlyList<EnrollmentResponseDTO>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetMine(CancellationToken cancellationToken) =>
        ExecuteAsync(() => enrollmentService.GetMineAsync(cancellationToken), Ok);

    [Authorize(Roles = "Administrador")]
    [HttpGet("enrollments")]
    [ProducesResponseType<IReadOnlyList<EnrollmentResponseDTO>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetAll([FromQuery] EnrollmentStatus? status, CancellationToken cancellationToken) =>
        ExecuteAsync(() => enrollmentService.GetAllAsync(status, cancellationToken), Ok);

    [Authorize(Roles = "Alumno,Administrador")]
    [HttpGet("enrollments/{id:guid}")]
    [ProducesResponseType<EnrollmentResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        ExecuteAsync(() => enrollmentService.GetByIdAsync(id, cancellationToken), Ok);

    [Authorize(Roles = "Alumno")]
    [HttpGet("enrollments/{id:guid}/course-content")]
    [ProducesResponseType<PublishedCourseResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status409Conflict)]
    public Task<IActionResult> GetCourseContent(Guid id, CancellationToken cancellationToken) =>
        ExecuteAsync(() => enrollmentService.GetCourseContentAsync(id, cancellationToken), Ok);

    [Authorize(Roles = "Administrador")]
    [HttpPost("enrollments/{id:guid}/decision")]
    [ProducesResponseType<EnrollmentResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Resolve(Guid id, EnrollmentDecisionDTO dto, CancellationToken cancellationToken) =>
        ExecuteAsync(() => enrollmentService.ResolveAsync(id, dto, cancellationToken), Ok);

    [Authorize(Roles = "Alumno,Administrador")]
    [HttpPost("enrollments/{id:guid}/cancel")]
    [ProducesResponseType<EnrollmentResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken) =>
        ExecuteAsync(() => enrollmentService.CancelAsync(id, cancellationToken), Ok);

    [Authorize(Roles = "Alumno,Profesor,Administrador")]
    [HttpGet("enrollments/{id:guid}/progress")]
    [ProducesResponseType<ProgressResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status409Conflict)]
    public Task<IActionResult> GetProgress(Guid id, CancellationToken cancellationToken) =>
        ExecuteAsync(() => progressService.GetAsync(id, cancellationToken), Ok);

    [Authorize(Roles = "Alumno")]
    [HttpPut("enrollments/{id:guid}/modules/{moduleId:guid}/completion")]
    [ProducesResponseType<ProgressResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status409Conflict)]
    public Task<IActionResult> CompleteModule(Guid id, Guid moduleId, CancellationToken cancellationToken) =>
        ExecuteAsync(() => progressService.CompleteModuleAsync(id, moduleId, cancellationToken), Ok);

    [Authorize(Roles = "Profesor,Administrador")]
    [HttpGet("courses/{courseId:guid}/students/progress")]
    [ProducesResponseType<IReadOnlyList<StudentProgressResponseDTO>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status409Conflict)]
    public Task<IActionResult> GetCourseProgress(Guid courseId, CancellationToken cancellationToken) =>
        ExecuteAsync(() => progressService.GetCourseProgressAsync(courseId, cancellationToken), Ok);

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

