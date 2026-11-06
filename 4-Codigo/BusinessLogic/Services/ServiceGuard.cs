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
        var validDni = Required(dni, "DNI", UserFieldLimits.Dni);
        if (validDni.Length is not (7 or 8) || !validDni.All(c => c is >= '0' and <= '9'))
            throw new LumenValidationException("El DNI debe tener 7 u 8 dígitos.");
        var validPhone = Required(phone, "teléfono", UserFieldLimits.Phone);
        if (!validPhone.All(c => c is >= '0' and <= '9' or '+' or ' ' or '-' or '(' or ')') ||
            validPhone.Count(c => c == '+') > 1 || (validPhone.Contains('+') && !validPhone.StartsWith("+54", StringComparison.Ordinal)) ||
            (validPhone.Contains('+') && validPhone[0] != '+') ||
            validPhone.Count(c => c is >= '0' and <= '9') is < 10 or > 15)
            throw new LumenValidationException("El teléfono debe tener entre 10 y 15 dígitos y usar un formato nacional o +54.");
        Required(address, "dirección", UserFieldLimits.Address);
        var validPostalCode = Required(postalCode, "código postal", UserFieldLimits.PostalCode);
        if (!System.Text.RegularExpressions.Regex.IsMatch(validPostalCode, @"^(?:[0-9]{4}|[A-Za-z][0-9]{4}[A-Za-z]{3})$"))
            throw new LumenValidationException("El código postal debe tener cuatro dígitos o el formato CPA argentino.");
        if (string.IsNullOrWhiteSpace(password) || password.Length is < 8 or > 128)
            throw new LumenValidationException("La contraseña debe tener entre 8 y 128 caracteres.");
    }

}

