using System.Security.Cryptography;
using Lumen.BusinessLogic.Abstractions;
using Lumen.BusinessLogic.Interfaces;
using Lumen.DataAccess.Entities;
using Lumen.DataAccess.Repositories;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Lumen.Shared.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace Lumen.BusinessLogic.Services;

public sealed class AuthService(
    IUserRepository userRepository,
    IPasswordHasher<User> passwordHasher,
    ITokenService tokenService) : IAuthService
{
    private static readonly User MissingAccount = CreateMissingAccount();

    public async Task<UserResponseDTO> RegisterAsync(UserCreateDTO dto, CancellationToken cancellationToken = default)
    {
        if (dto.Role is not (UserRole.Alumno or UserRole.Profesor))
        {
            throw new PublicRegistrationRoleNotAllowedException();
        }

        var normalizedEmail = ServiceGuard.NormalizeNewEmail(dto.Email);
        ServiceGuard.ValidatePersonalData(dto.FirstName, dto.LastName, dto.Dni, dto.Phone, dto.Address, dto.PostalCode, dto.Password);
        if (await userRepository.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            throw new DuplicateEmailException(dto.Email);
        }

        var user = new User
        {
            Email = dto.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            Role = dto.Role,
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

    public async Task<LoginResponseDTO> LoginAsync(LoginRequestDTO dto, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = ServiceGuard.NormalizeEmail(dto.Email);
        ServiceGuard.Required(dto.Password, "contraseña");
        var user = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        var account = user ?? MissingAccount;
        var verification = passwordHasher.VerifyHashedPassword(account, account.PasswordHash, dto.Password);
        if (user is null || verification == PasswordVerificationResult.Failed)
        {
            throw new InvalidCredentialsException();
        }
        if (!user.IsActive)
        {
            throw new InactiveUserException();
        }
        var token = tokenService.Generate(user);
        return new LoginResponseDTO(token.AccessToken, token.ExpiresAtUtc, MapToResponseDTO(user));
    }

    private static User CreateMissingAccount()
    {
        // Match the default Identity hasher used for stored accounts, without persisting this account.
        var account = new User();
        account.PasswordHash = new PasswordHasher<User>().HashPassword(account,
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        return account;
    }

    private static UserResponseDTO MapToResponseDTO(User user) =>
        new(user.Id, user.Email, user.Role, user.FirstName, user.LastName, user.Dni, user.Phone, user.Address, user.PostalCode, user.IsActive, user.CreatedAtUtc);
}

