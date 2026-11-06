namespace Lumen.Shared.Exceptions;

public sealed class CourseEditPreconditionRequiredException()
    : Exception("Consultá el curso y enviá su ETag específico en la cabecera If-Match.");
