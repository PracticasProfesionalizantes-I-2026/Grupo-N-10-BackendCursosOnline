namespace Lumen.Shared.DTOs;

public sealed record LoginRequestDTO(string Email, string Password);

public sealed record LoginResponseDTO(
    string AccessToken,
    DateTime ExpiresAtUtc,
    UserResponseDTO User);

