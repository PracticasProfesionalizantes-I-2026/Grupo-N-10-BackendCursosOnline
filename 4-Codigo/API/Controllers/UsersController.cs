using Lumen.BusinessLogic.Interfaces;
using Lumen.Shared.DTOs;
using Lumen.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.API.Controllers;

[ApiController]
[Authorize(Roles = "Administrador")]
[Route("api/users")]
public sealed class UsersController(IUserService service) : ControllerBase
{
    [HttpGet]
    public Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        ExecuteAsync(() => service.GetAllAsync(cancellationToken), Ok);

    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        ExecuteAsync(() => service.GetByIdAsync(id, cancellationToken), Ok);

    [HttpPost("administrators")]
    public Task<IActionResult> CreateAdministrator(AdministratorCreateDTO dto, CancellationToken cancellationToken) =>
        ExecuteAsync(() => service.CreateAdministratorAsync(dto, cancellationToken),
            value => CreatedAtAction(nameof(GetById), new { id = value.Id }, value));

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

