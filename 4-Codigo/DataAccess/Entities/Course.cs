using Lumen.Shared.Enums;

namespace Lumen.DataAccess.Entities;

public sealed class Course
{
    public Guid Id { get; set; }
    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public Guid? OwnerProfessorId { get; set; }
    public User? OwnerProfessor { get; set; }
    public CourseOperationalStatus OperationalStatus { get; set; } = CourseOperationalStatus.Activo;
    public Guid? WorkingRevisionId { get; set; }
    public CourseRevision? WorkingRevision { get; set; }
    public Guid? PublishedRevisionId { get; set; }
    public CourseRevision? PublishedRevision { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public ICollection<CourseRevision> Revisions { get; set; } = [];
    public ICollection<Enrollment> Enrollments { get; set; } = [];
}
