using System.Net.Mail;
using Lumen.BusinessLogic.Abstractions;
using Lumen.Shared.Enums;
using Lumen.Shared.Exceptions;
using Lumen.Shared.Validation;

namespace Lumen.BusinessLogic.Services;

internal static class ServiceGuard
{
    public static void Authenticated(ICurrentUser currentUser)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new ForbiddenOperationException("Se requiere un usuario autenticado.");
        }
    }

    public static void Role(ICurrentUser currentUser, params UserRole[] roles)
    {
        Authenticated(currentUser);
        if (!roles.Contains(currentUser.Role))
        {
            throw new ForbiddenOperationException();
        }
    }

    public static string Required(string? value, string field, int? maximumLength = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new LumenValidationException($"El campo {field} es obligatorio.");
        }
        var trimmed = value.Trim();
        if (maximumLength is int maximum && trimmed.Length > maximum)
        {
            throw new LumenValidationException($"El campo {field} admite hasta {maximum} caracteres.");
        }
        return trimmed;
    }

    public static string NormalizeEmail(string? email)
    {
        var value = Required(email, "email", UserFieldLimits.Email);
        if (!value.Contains('@'))
        {
            throw new LumenValidationException("El email no posee un formato válido.");
        }
        return value.ToUpperInvariant();
    }

    public static string NormalizeNewEmail(string? email)
    {
        var value = Required(email, "email", UserFieldLimits.Email);
        if (!MailAddress.TryCreate(value, out var address) ||
            !string.Equals(address.Address, value, StringComparison.Ordinal))
        {
            throw new LumenValidationException("El email no posee un formato válido.");
        }
        return value.ToUpperInvariant();
    }

    public static void ValidatePersonalData(
        string? firstName, string? lastName, string? dni, string? phone,
        string? address, string? postalCode, string? password)
    {
        Required(firstName, "nombre", UserFieldLimits.Name);
        Required(lastName, "apellido", UserFieldLimits.Name);
        ValidateSingleLine(dni, "DNI", UserFieldLimits.Dni);
        ValidateSingleLine(phone, "teléfono", UserFieldLimits.Phone);
        Required(address, "dirección", UserFieldLimits.Address);
        Required(postalCode, "código postal", UserFieldLimits.PostalCode);
        Required(password, "contraseña");
    }

    private static void ValidateSingleLine(string? value, string field, int maximumLength)
    {
        if (Required(value, field, maximumLength).Any(char.IsControl))
            throw new LumenValidationException($"El campo {field} contiene caracteres de control.");
    }
}

