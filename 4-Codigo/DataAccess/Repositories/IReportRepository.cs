namespace Lumen.DataAccess.Repositories;

public sealed record ReportCounts(int RegisteredUsers, int CreatedCourses, int RequestedEnrollments);

public interface IReportRepository
{
    Task<ReportCounts> GetCountsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
}

