using System.Text.Json.Serialization;

namespace Lumen.Shared.DTOs;

public sealed record ReportSummaryResponseDTO(
    DateTime From,
    DateTime To,
    int RegisteredUsers,
    int CreatedCourses,
    int RequestedEnrollments)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TimeZone { get; init; }
}

