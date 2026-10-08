using Microsoft.EntityFrameworkCore;

namespace Lumen.DataAccess.Repositories;

public sealed class ReportRepository(LumenDbContext context) : IReportRepository
{
    public async Task<ReportCounts> GetCountsAsync(DateTime fromUtc, DateTime? toUtcExclusive, CancellationToken cancellationToken = default)
    {
        var users = await context.Users.AsNoTracking().CountAsync(
            x => x.CreatedAtUtc >= fromUtc && (!toUtcExclusive.HasValue || x.CreatedAtUtc < toUtcExclusive.Value), cancellationToken);
        var courses = await context.Courses.AsNoTracking().CountAsync(
            x => x.CreatedAtUtc >= fromUtc && (!toUtcExclusive.HasValue || x.CreatedAtUtc < toUtcExclusive.Value), cancellationToken);
        var enrollments = await context.Enrollments.AsNoTracking().CountAsync(
            x => x.RequestedAtUtc >= fromUtc && (!toUtcExclusive.HasValue || x.RequestedAtUtc < toUtcExclusive.Value), cancellationToken);
        return new ReportCounts(users, courses, enrollments);
    }
}
