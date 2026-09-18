using Lumen.Shared.Enums;

namespace Lumen.DataAccess.Entities;

public sealed class CourseRevision
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int Version { get; set; }
    public CourseRevisionStatus Status { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Modality { get; set; } = string.Empty;
    public int MaxCapacity { get; set; }
    public int TotalDurationMinutes { get; set; }
    public List<string> LearningObjectives { get; set; } = [];
    public List<string> SuggestedPrerequisites { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public ICollection<CourseModule> Modules { get; set; } = [];
    public ICollection<CourseReview> Reviews { get; set; } = [];
}

