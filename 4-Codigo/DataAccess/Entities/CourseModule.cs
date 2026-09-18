namespace Lumen.DataAccess.Entities;

public sealed class CourseModule
{
    public Guid RevisionId { get; set; }
    public CourseRevision Revision { get; set; } = null!;
    public Guid Id { get; set; }
    public int Order { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public string Content { get; set; } = string.Empty;
    public List<string> Resources { get; set; } = [];
}

