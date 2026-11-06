using Lumen.API.Security;
using Lumen.BusinessLogic.Interfaces;
using Lumen.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Lumen.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService service) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<UserResponseDTO>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(UserCreateDTO dto, CancellationToken cancellationToken)
    {
        var value = await service.RegisterAsync(dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, value);
    }

    [HttpPost("login")]
    [EnableRateLimiting(LoginRateLimit.Policy)]
    [ProducesResponseType<LoginResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(LoginRequestDTO dto, CancellationToken cancellationToken) =>
        Ok(await service.LoginAsync(dto, cancellationToken));
}
