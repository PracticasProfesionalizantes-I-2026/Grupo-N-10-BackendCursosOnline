namespace Lumen.Shared.Exceptions;
public sealed class CourseRequiresModuleException() : LumenException("El curso debe poseer al menos un módulo antes de enviarse a revisión.");
