using Lumen.DataAccess.Entities;
using Lumen.Shared.Enums;
using Microsoft.AspNetCore.Identity;

namespace Lumen.BusinessLogic.Services;

internal static class UserFactory
{
    internal static User Create(string email, string password, UserRole role, string firstName, string lastName,
        string dni, string phone, string address, string postalCode, IPasswordHasher<User> passwordHasher)
    {
        var normalizedEmail = ServiceGuard.NormalizeNewEmail(email);
        ServiceGuard.ValidatePersonalData(firstName, lastName, dni, phone, address, postalCode, password);
        var user = new User
        {
            Email = email.Trim(),
            NormalizedEmail = normalizedEmail,
            Role = role,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Dni = dni.Trim(),
            Phone = phone.Trim(),
            Address = address.Trim(),
            PostalCode = postalCode.Trim().ToUpperInvariant(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, password);
        return user;
    }
}
