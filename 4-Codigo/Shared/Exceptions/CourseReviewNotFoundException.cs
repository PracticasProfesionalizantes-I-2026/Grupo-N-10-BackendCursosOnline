namespace Lumen.Shared.Exceptions;
public sealed class CourseReviewNotFoundException(Guid id) : LumenException($"No se encontró la auditoría '{id}'.");
