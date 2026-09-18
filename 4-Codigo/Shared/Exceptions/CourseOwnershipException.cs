namespace Lumen.Shared.Exceptions;
public sealed class CourseOwnershipException() : LumenException("El profesor solo puede gestionar cursos propios.");
