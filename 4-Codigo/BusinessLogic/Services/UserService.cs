using Lumen.BusinessLogic.Abstractions;
using Lumen.BusinessLogic.Interfaces;
using Lumen.DataAccess.Entities;
using Lumen.DataAccess.Repositories;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Lumen.Shared.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace Lumen.BusinessLogic.Services;

public sealed class UserService(
    IUserRepository userRepository,
    IPasswordHasher<User> passwordHasher,
    ICurrentUser currentUser) : IUserService
{
    public async Task<IReadOnlyList<UserResponseDTO>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        ServiceGuard.Role(currentUser, UserRole.Administrador);
        var users = await userRepository.GetAllAsync(cancellationToken);
        return users.Select(MapToResponseDTO).ToList();
    }

    public async Task<UserResponseDTO> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Role(currentUser, UserRole.Administrador);
        var user = await userRepository.GetByIdAsync(id, cancellationToken) ?? throw new UserNotFoundException(id);
        return MapToResponseDTO(user);
    }

    public async Task<UserResponseDTO> CreateAdministratorAsync(AdministratorCreateDTO dto, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Role(currentUser, UserRole.Administrador);
        var normalizedEmail = ServiceGuard.NormalizeEmail(dto.Email);
        ServiceGuard.Required(dto.Password, "contraseña");
        ServiceGuard.Required(dto.FirstName, "nombre");
        ServiceGuard.Required(dto.LastName, "apellido");
        ServiceGuard.Required(dto.Dni, "DNI");
        ServiceGuard.Required(dto.Phone, "teléfono");
        ServiceGuard.Required(dto.Address, "dirección");
        ServiceGuard.Required(dto.PostalCode, "código postal");
        if (await userRepository.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            throw new DuplicateEmailException(dto.Email);
        }

        var user = new User
        {
            Email = dto.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            Role = UserRole.Administrador,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Dni = dto.Dni.Trim(),
            Phone = dto.Phone.Trim(),
            Address = dto.Address.Trim(),
            PostalCode = dto.PostalCode.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, dto.Password);
        await userRepository.CreateAsync(user, cancellationToken);
        return MapToResponseDTO(user);
    }

    private static UserResponseDTO MapToResponseDTO(User user) =>
        new(user.Id, user.Email, user.Role, user.FirstName, user.LastName, user.Dni, user.Phone, user.Address, user.PostalCode, user.IsActive, user.CreatedAtUtc);
}

