namespace Lumen.DataAccess;

public static class DemoAccounts
{
    public const string StudentEmail = "alumno@lumen.local";
    public const string TeacherEmail = "profesor@lumen.local";
    public const string AdministratorEmail = "admin@lumen.local";

    public static bool IsAllowedIn(string? environmentName) =>
        string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(environmentName, "Testing", StringComparison.OrdinalIgnoreCase);

    public static bool IsReserved(string? email) =>
        string.Equals(email, StudentEmail, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(email, TeacherEmail, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(email, AdministratorEmail, StringComparison.OrdinalIgnoreCase);
}
