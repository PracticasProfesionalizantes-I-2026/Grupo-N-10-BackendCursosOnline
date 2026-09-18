using Lumen.Shared.Enums;

namespace Lumen.DataAccess.Entities;

public sealed class CourseReview
{
    public Guid Id { get; set; }
    public Guid CourseRevisionId { get; set; }
    public CourseRevision CourseRevision { get; set; } = null!;
    public CourseReviewType Type { get; set; }
    public CourseReviewDecision? Decision { get; set; }
    public string? Observation { get; set; }
    public Guid RequestedByUserId { get; set; }
    public User RequestedByUser { get; set; } = null!;
    public Guid? ReviewedByUserId { get; set; }
    public User? ReviewedByUser { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
}

