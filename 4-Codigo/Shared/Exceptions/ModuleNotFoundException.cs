namespace Lumen.Shared.Exceptions;
public sealed class ModuleNotFoundException(Guid id) : LumenException($"No se encontró el módulo '{id}'.");
