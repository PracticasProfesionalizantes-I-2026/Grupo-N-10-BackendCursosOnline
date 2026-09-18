using Lumen.BusinessLogic.Abstractions;
using Lumen.Shared.Enums;
using Lumen.Shared.Exceptions;

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

    public static string Required(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new LumenValidationException($"El campo {field} es obligatorio.");
        }
        return value.Trim();
    }

    public static string NormalizeEmail(string? email)
    {
        var value = Required(email, "email");
        if (!value.Contains('@') || value.Length > 254)
        {
            throw new LumenValidationException("El email no posee un formato válido.");
        }
        return value.ToUpperInvariant();
    }
}

