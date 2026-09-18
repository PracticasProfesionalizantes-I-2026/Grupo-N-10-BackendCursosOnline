namespace Lumen.Shared.Exceptions;
public sealed class ForbiddenOperationException(string message = "El usuario no posee permisos para realizar esta operación.") : LumenException(message);
