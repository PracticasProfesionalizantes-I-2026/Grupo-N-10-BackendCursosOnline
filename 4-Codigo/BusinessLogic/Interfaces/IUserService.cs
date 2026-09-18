using Lumen.Shared.DTOs;

namespace Lumen.BusinessLogic.Interfaces;

public interface IUserService
{
    Task<IReadOnlyList<UserResponseDTO>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UserResponseDTO> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserResponseDTO> CreateAdministratorAsync(AdministratorCreateDTO dto, CancellationToken cancellationToken = default);
}

