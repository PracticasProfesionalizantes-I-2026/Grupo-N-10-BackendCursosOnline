namespace Lumen.Shared.Exceptions;
public sealed class CourseNotFoundException(Guid id) : LumenException($"No se encontró el curso '{id}'.");
