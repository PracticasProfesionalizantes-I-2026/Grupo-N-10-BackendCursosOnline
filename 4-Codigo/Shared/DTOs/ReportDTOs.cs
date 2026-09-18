namespace Lumen.Shared.DTOs;

public sealed record ReportFilterDTO(DateTime From, DateTime To);

public sealed record ReportSummaryResponseDTO(
    DateTime From,
    DateTime To,
    int RegisteredUsers,
    int CreatedCourses,
    int RequestedEnrollments);

