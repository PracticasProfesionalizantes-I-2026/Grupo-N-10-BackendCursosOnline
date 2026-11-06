using System.Globalization;
using Lumen.BusinessLogic.Interfaces;
using Lumen.Shared.DTOs;
using Lumen.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.API.Controllers;

[ApiController]
[Authorize(Roles = "Administrador")]
[Route("api/reports")]
public sealed class ReportsController(IReportService service) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType<ReportSummaryResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponseDTO>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] string? timeZone, CancellationToken cancellationToken)
    {
        if (!from.HasValue || !to.HasValue)
        {
            var message = !from.HasValue && !to.HasValue
                ? "Faltan los parámetros obligatorios: from y to."
                : !from.HasValue
                    ? "Falta el parámetro obligatorio: from."
                    : "Falta el parámetro obligatorio: to.";
            throw new LumenValidationException(message);
        }

        if (timeZone is null)
            return Ok(await service.GetSummaryAsync(from.Value, to.Value, cancellationToken));

        if (!DateOnly.TryParseExact(Request.Query["from"].ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var start) ||
            !DateOnly.TryParseExact(Request.Query["to"].ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var end))
        {
            throw new LumenValidationException("Con timeZone, from y to deben ser fechas civiles en formato AAAA-MM-DD.");
        }

        if ((!TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZone, out _) && timeZone != "UTC") ||
            !TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out var zone))
        {
            throw new LumenValidationException("timeZone debe ser una zona IANA disponible, por ejemplo America/Argentina/Buenos_Aires.");
        }

        return Ok(await service.GetSummaryAsync(start.ToDateTime(TimeOnly.MinValue),
            end.ToDateTime(TimeOnly.MinValue), cancellationToken, zone));
    }
}
