using Microsoft.EntityFrameworkCore;

namespace Lumen.DataAccess.Repositories;

public sealed class ReportRepository(LumenDbContext context) : IReportRepository
{
    public async Task<ReportCounts> GetCountsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var users = await context.Users.AsNoTracking().CountAsync(x => x.CreatedAtUtc >= fromUtc && x.CreatedAtUtc <= toUtc, cancellationToken);
        var courses = await context.Courses.AsNoTracking().CountAsync(x => x.CreatedAtUtc >= fromUtc && x.CreatedAtUtc <= toUtc, cancellationToken);
        var enrollments = await context.Enrollments.AsNoTracking().CountAsync(x => x.RequestedAtUtc >= fromUtc && x.RequestedAtUtc <= toUtc, cancellationToken);
        return new ReportCounts(users, courses, enrollments);
    }
}
