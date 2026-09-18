namespace Lumen.DataAccess.Entities;

public sealed class EnrollmentModuleCompletion
{
    public Guid EnrollmentId { get; set; }
    public Enrollment Enrollment { get; set; } = null!;
    public Guid ModuleId { get; set; }
    public DateTime CompletedAtUtc { get; set; }
}

