namespace Lumen.Shared.Exceptions;
public sealed class DuplicateEmailException(string email) : LumenException($"Ya existe una cuenta registrada con el email '{email}'.");
