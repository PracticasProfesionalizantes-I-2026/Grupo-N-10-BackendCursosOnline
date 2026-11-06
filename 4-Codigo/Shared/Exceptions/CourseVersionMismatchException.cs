namespace Lumen.Shared.Exceptions;

public sealed class CourseVersionMismatchException()
    : Exception("El curso cambió desde la última consulta. Volvé a cargarlo antes de editar.");
