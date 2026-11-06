namespace Lumen.DataAccess.Entities;

public sealed class CourseReviewSnapshot
{
    public Guid ReviewId { get; set; }
    public string ContentJson { get; set; } = string.Empty;
}
