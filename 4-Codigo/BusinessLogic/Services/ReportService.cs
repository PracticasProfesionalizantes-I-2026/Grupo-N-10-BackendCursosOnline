using Lumen.BusinessLogic.Abstractions;
using Lumen.BusinessLogic.Interfaces;
using Lumen.DataAccess.Repositories;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Lumen.Shared.Exceptions;

namespace Lumen.BusinessLogic.Services;

public sealed class ReportService(
    IReportRepository reportRepository,
    ICurrentUser currentUser) : IReportService
{
    public async Task<ReportSummaryResponseDTO> GetSummaryAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Role(currentUser, UserRole.Administrador);
        if (from > to)
        {
            throw new InvalidDateRangeException();
        }
        var fromUtc = DateTime.SpecifyKind(from, DateTimeKind.Utc);
        var toUtc = DateTime.SpecifyKind(to, DateTimeKind.Utc);
        var counts = await reportRepository.GetCountsAsync(fromUtc, toUtc, cancellationToken);
        return new ReportSummaryResponseDTO(fromUtc, toUtc, counts.RegisteredUsers, counts.CreatedCourses, counts.RequestedEnrollments);
    }
}
