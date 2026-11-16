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
        // [ApiController] normally short-circuits invalid binding; keep the order explicit.
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

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
        {
            // Parse the original query as an instant. ASP.NET's DateTime binder can
            // convert an explicit offset using the host's local time zone.
            if (!DateTimeOffset.TryParse(Request.Query["from"], CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal, out var fromInstant) ||
                !DateTimeOffset.TryParse(Request.Query["to"], CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal, out var toInstant))
                throw new LumenValidationException("from y to deben ser fechas válidas.");
            return Ok(await service.GetSummaryAsync(fromInstant.UtcDateTime, toInstant.UtcDateTime, cancellationToken));
        }

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
