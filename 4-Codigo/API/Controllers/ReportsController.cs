using Lumen.BusinessLogic.Interfaces;
using Lumen.Shared.DTOs;
using Lumen.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.API.Controllers;

[ApiController]
[Authorize(Roles = "Administrador")]
[Route("api/reports")]
public sealed class ReportsController(IReportService service) : ControllerBase
{
    [HttpGet("summary")]
    public Task<IActionResult> GetSummary([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken cancellationToken) =>
        ExecuteAsync(() => service.GetSummaryAsync(from, to, cancellationToken), Ok);

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
