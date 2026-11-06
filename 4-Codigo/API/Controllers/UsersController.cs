using Lumen.BusinessLogic.Interfaces;
using Lumen.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.API.Controllers;

[ApiController]
[Authorize(Roles = "Administrador")]
[Route("api/users")]
public sealed class UsersController(IUserService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UserResponseDTO>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await service.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<UserResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetByIdAsync(id, cancellationToken));

    [HttpPost("administrators")]
    [ProducesResponseType<UserResponseDTO>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAdministrator(AdministratorCreateDTO dto, CancellationToken cancellationToken)
    {
        var value = await service.CreateAdministratorAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = value.Id }, value);
    }
}
