namespace Lumen.Shared.Exceptions;
public sealed class UserNotFoundException(Guid id) : LumenException($"No se encontró el usuario '{id}'.");
