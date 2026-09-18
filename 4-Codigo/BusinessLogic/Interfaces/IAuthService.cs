using Lumen.Shared.DTOs;

namespace Lumen.BusinessLogic.Interfaces;

public interface IAuthService
{
    Task<UserResponseDTO> RegisterAsync(UserCreateDTO dto, CancellationToken cancellationToken = default);
    Task<LoginResponseDTO> LoginAsync(LoginRequestDTO dto, CancellationToken cancellationToken = default);
}

