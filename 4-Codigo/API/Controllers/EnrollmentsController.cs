using Lumen.BusinessLogic.Interfaces;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
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
    public async Task<IActionResult> Create(EnrollmentCreateDTO dto, CancellationToken cancellationToken)
    {
        var value = await enrollmentService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = value.Id }, value);
    }

    [Authorize(Roles = "Alumno")]
    [HttpGet("enrollments/mine")]
    [ProducesResponseType<IReadOnlyList<EnrollmentResponseDTO>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken) =>
        Ok(await enrollmentService.GetMineAsync(cancellationToken));

    [Authorize(Roles = "Administrador")]
    [HttpGet("enrollments")]
    [ProducesResponseType<IReadOnlyList<EnrollmentResponseDTO>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] EnrollmentStatus? status, CancellationToken cancellationToken) =>
        Ok(await enrollmentService.GetAllAsync(status, cancellationToken));

    [Authorize(Roles = "Alumno,Administrador")]
    [HttpGet("enrollments/{id:guid}")]
    [ProducesResponseType<EnrollmentResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await enrollmentService.GetByIdAsync(id, cancellationToken));

    [Authorize(Roles = "Alumno")]
    [HttpGet("enrollments/{id:guid}/course-content")]
    [ProducesResponseType<PublishedCourseResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetCourseContent(Guid id, CancellationToken cancellationToken) =>
        Ok(await enrollmentService.GetCourseContentAsync(id, cancellationToken));

    [Authorize(Roles = "Administrador")]
    [HttpPost("enrollments/{id:guid}/decision")]
    [ProducesResponseType<EnrollmentResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Resolve(Guid id, EnrollmentDecisionDTO dto, CancellationToken cancellationToken) =>
        Ok(await enrollmentService.ResolveAsync(id, dto, cancellationToken));

    [Authorize(Roles = "Alumno,Administrador")]
    [HttpPost("enrollments/{id:guid}/cancel")]
    [ProducesResponseType<EnrollmentResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken) =>
        Ok(await enrollmentService.CancelAsync(id, cancellationToken));

    [Authorize(Roles = "Alumno,Profesor,Administrador")]
    [HttpGet("enrollments/{id:guid}/progress")]
    [ProducesResponseType<ProgressResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetProgress(Guid id, CancellationToken cancellationToken) =>
        Ok(await progressService.GetAsync(id, cancellationToken));

    [Authorize(Roles = "Alumno")]
    [HttpPut("enrollments/{id:guid}/modules/{moduleId:guid}/completion")]
    [ProducesResponseType<ProgressResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CompleteModule(Guid id, Guid moduleId, CancellationToken cancellationToken) =>
        Ok(await progressService.CompleteModuleAsync(id, moduleId, cancellationToken));

    [Authorize(Roles = "Profesor,Administrador")]
    [HttpGet("courses/{courseId:guid}/students/progress")]
    [ProducesResponseType<IReadOnlyList<StudentProgressResponseDTO>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetCourseProgress(Guid courseId, CancellationToken cancellationToken) =>
        Ok(await progressService.GetCourseProgressAsync(courseId, cancellationToken));
}
