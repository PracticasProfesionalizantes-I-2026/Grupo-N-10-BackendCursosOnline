using Lumen.Shared.Enums;

namespace Lumen.Shared.DTOs;

public sealed record UserCreateDTO(
    string Email,
    string Password,
    UserRole Role,
    string FirstName,
    string LastName,
    string Dni,
    string Phone,
    string Address,
    string PostalCode);

public sealed record AdministratorCreateDTO(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Dni,
    string Phone,
    string Address,
    string PostalCode);

public sealed record UserResponseDTO(
    Guid Id,
    string Email,
    UserRole Role,
    string FirstName,
    string LastName,
    string Dni,
    string Phone,
    string Address,
    string PostalCode,
    bool IsActive,
    DateTime CreatedAtUtc);

