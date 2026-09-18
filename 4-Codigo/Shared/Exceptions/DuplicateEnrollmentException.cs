namespace Lumen.Shared.Exceptions;
public sealed class DuplicateEnrollmentException() : LumenException("Ya existe una inscripción pendiente o aprobada para este curso.");
