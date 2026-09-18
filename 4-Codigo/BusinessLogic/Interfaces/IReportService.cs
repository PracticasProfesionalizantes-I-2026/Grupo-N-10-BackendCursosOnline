using Lumen.Shared.DTOs;

namespace Lumen.BusinessLogic.Interfaces;

public interface IReportService
{
    Task<ReportSummaryResponseDTO> GetSummaryAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
}

