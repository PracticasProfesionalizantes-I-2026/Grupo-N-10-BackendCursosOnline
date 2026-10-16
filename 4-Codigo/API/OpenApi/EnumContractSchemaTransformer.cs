using System.Text.Json.Nodes;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Lumen.API.OpenApi;

internal sealed class EnumContractSchemaTransformer : IOpenApiSchemaTransformer, IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        var type = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type;
        if (type.IsEnum)
        {
            var nullable = (schema.Type & JsonSchemaType.Null) == JsonSchemaType.Null;
            schema.Type = JsonSchemaType.String | (nullable ? JsonSchemaType.Null : 0);
            schema.Format = null;
            schema.Enum = Enum.GetNames(type).Select(name => (JsonNode)JsonValue.Create(name)!).ToList();
            if (nullable)
            {
                schema.Enum.Add(null!);
            }
        }

        return Task.CompletedTask;
    }

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        // Apply input variants after schema generation, so output enum references stay string-only.
        // MVC accepts numeric values too; business rules restrict public roles and decisions.
        var schemas = document.Components!.Schemas!;
        schemas[nameof(UserCreateDTO)].Properties!["role"] = InputEnum(UserRole.Alumno, UserRole.Profesor);
        schemas[nameof(EnrollmentDecisionDTO)].Properties!["decision"] = InputEnum(EnrollmentStatus.Aprobada, EnrollmentStatus.Rechazada);
        schemas[nameof(CourseReviewDecisionDTO)].Properties!["decision"] = InputEnum(Enum.GetValues<CourseReviewDecision>());
        foreach (var operation in document.Paths["/api/enrollments"].Operations!.Values)
        {
            foreach (var parameter in (operation.Parameters ?? []).OfType<OpenApiParameter>())
            {
                if (parameter.In == ParameterLocation.Query && parameter.Name == "status")
                {
                    parameter.Schema = InputEnum(Enum.GetValues<EnrollmentStatus>());
                }
            }
        }
        return Task.CompletedTask;
    }

    private static OpenApiSchema InputEnum<T>(params T[] values) where T : struct, Enum => new()
    {
        Description = "Se recomiendan los nombres indicados. También se aceptan sus valores numéricos " +
            "(como números o cadenas); los nombres no distinguen mayúsculas de minúsculas.",
        AnyOf =
        [
            new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Enum = values.Select(value => (JsonNode)JsonValue.Create(value.ToString())!).ToList()
            },
            new OpenApiSchema
            {
                Type = JsonSchemaType.Integer,
                Enum = values.Select(value => (JsonNode)JsonValue.Create(Convert.ToInt32(value))!).ToList()
            },
            new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Enum = values.Select(value => (JsonNode)JsonValue.Create(Convert.ToInt32(value).ToString(
                    System.Globalization.CultureInfo.InvariantCulture))!).ToList()
            }
        ]
    };
}
