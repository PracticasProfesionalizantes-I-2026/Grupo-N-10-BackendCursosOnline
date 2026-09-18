namespace Lumen.Shared.Exceptions;
public sealed class DependencyConflictException(string message) : LumenException(message);
