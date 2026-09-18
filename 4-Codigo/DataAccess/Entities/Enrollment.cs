using Lumen.Shared.Enums;

namespace Lumen.DataAccess.Entities;

public sealed class Enrollment
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public User Student { get; set; } = null!;
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public EnrollmentStatus Status { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public User? ResolvedByUser { get; set; }
    public DateTime? CanceledAtUtc { get; set; }
    public Guid? CanceledByUserId { get; set; }
    public User? CanceledByUser { get; set; }
    public ICollection<EnrollmentModuleCompletion> ModuleCompletions { get; set; } = [];
}

