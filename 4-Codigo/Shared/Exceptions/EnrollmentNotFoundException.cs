namespace Lumen.Shared.Exceptions;
public sealed class EnrollmentNotFoundException(Guid id) : LumenException($"No se encontró la inscripción '{id}'.");
