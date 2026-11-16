using System.Text.Json.Serialization;

namespace Lumen.Shared.DTOs;

/// <summary>Recuento por días civiles, incluidos ambos extremos seleccionados.</summary>
/// <param name="From">Instante UTC de inicio del filtro; con timeZone es la medianoche del primer día civil seleccionado.</param>
/// <param name="To">Instante UTC seleccionado como límite final; el recuento incluye hasta el final de ese día y termina al inicio del siguiente, exclusivo. Con timeZone es la medianoche del último día civil seleccionado.</param>
/// <param name="RegisteredUsers">Usuarios registrados en el intervalo.</param>
/// <param name="CreatedCourses">Cursos creados en el intervalo.</param>
/// <param name="RequestedEnrollments">Inscripciones solicitadas en el intervalo.</param>
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

