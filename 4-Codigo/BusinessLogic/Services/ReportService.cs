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
    public async Task<ReportSummaryResponseDTO> GetSummaryAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default,
        TimeZoneInfo? timeZone = null)
    {
        ServiceGuard.Role(currentUser, UserRole.Administrador);
        if (from > to)
        {
            throw new InvalidDateRangeException();
        }
        if (timeZone is not null)
        {
            if (to.Date == DateTime.MaxValue.Date)
                throw new LumenValidationException("La fecha Hasta debe admitir un día siguiente en la zona seleccionada.");

            var start = StartOfDayUtc(from, timeZone);
            var lastDayStart = StartOfDayUtc(to, timeZone);
            var end = StartOfDayUtc(to.AddDays(1), timeZone);
            var civilCounts = await reportRepository.GetCountsAsync(start, end, cancellationToken);
            return new ReportSummaryResponseDTO(start, lastDayStart, civilCounts.RegisteredUsers,
                civilCounts.CreatedCourses, civilCounts.RequestedEnrollments) { TimeZone = timeZone.Id };
        }
        var fromUtc = DateTime.SpecifyKind(from, DateTimeKind.Utc);
        var toUtc = DateTime.SpecifyKind(to, DateTimeKind.Utc);
        // Hasta incluye el día completo; el día máximo representable no tiene un siguiente día.
        DateTime? toUtcExclusive = toUtc.Date == DateTime.MaxValue.Date ? null : toUtc.Date.AddDays(1);
        var counts = await reportRepository.GetCountsAsync(fromUtc, toUtcExclusive, cancellationToken);
        return new ReportSummaryResponseDTO(fromUtc, toUtc, counts.RegisteredUsers, counts.CreatedCourses, counts.RequestedEnrollments);
    }

    private static DateTime StartOfDayUtc(DateTime date, TimeZoneInfo timeZone)
    {
        var midnight = DateTime.SpecifyKind(date.Date, DateTimeKind.Unspecified);
        if (timeZone.IsInvalidTime(midnight))
            throw new LumenValidationException("La medianoche de un límite no existe en la zona seleccionada.");
        var offset = timeZone.IsAmbiguousTime(midnight)
            ? timeZone.GetAmbiguousTimeOffsets(midnight).Max()
            : timeZone.GetUtcOffset(midnight);
        try
        {
            return new DateTimeOffset(midnight, offset).UtcDateTime;
        }
        catch (ArgumentException)
        {
            throw new LumenValidationException("La fecha queda fuera del rango representable en UTC.");
        }
    }
}
