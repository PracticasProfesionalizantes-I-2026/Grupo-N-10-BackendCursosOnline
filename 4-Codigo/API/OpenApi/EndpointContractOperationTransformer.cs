using Lumen.API.Controllers;
using Lumen.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Lumen.API.OpenApi;

internal sealed class EndpointContractOperationTransformer : IOpenApiOperationTransformer
{
    public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        // Object/list responses use the JSON formatter; the string formatter cannot write these DTOs.
        foreach (var response in operation.Responses?.Values.AsEnumerable() ?? [])
        {
            response.Content?.Remove("text/plain");
        }
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        var authorization = metadata.OfType<IAuthorizeData>().ToArray();
        operation.Security = [];
        if (!metadata.OfType<IAllowAnonymous>().Any() && authorization.Length > 0)
        {
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
            });
            operation.Responses ??= new OpenApiResponses();
            operation.Responses.TryAdd("401", new OpenApiResponse
            {
                Description = "JWT ausente, inválido o vencido. Sin cuerpo; desafío Bearer."
            });
            var roles = authorization.Where(x => !string.IsNullOrWhiteSpace(x.Roles)).Select(x => x.Roles);
            var roleDescription = string.Join("; ", roles);
            if (roleDescription.Length > 0)
            {
                operation.Description = $"{operation.Description}\nRoles admitidos: {roleDescription}.".Trim();
                operation.Responses.TryAdd("403", new OpenApiResponse
                {
                    Description = "El usuario no posee el rol requerido. Sin cuerpo."
                });
            }
            if (operation.Responses.TryGetValue("403", out var forbidden) && forbidden.Content?.Count > 0)
            {
                forbidden.Description = "Acceso denegado por negocio (ApiErrorResponseDTO), " +
                    "o por autorización de roles (sin cuerpo).";
            }
        }

        // ApiController can reject binding before the action, independently of business validation.
        if (context.Description.ParameterDescriptions.Any(x => x.Source == BindingSource.Body || x.Source == BindingSource.Query))
        {
            operation.Responses ??= new OpenApiResponses();
            var badRequest = new OpenApiResponse { Content = new Dictionary<string, OpenApiMediaType>() };
            operation.Responses["400"] = badRequest;
            var businessError = context.Description.SupportedResponseTypes.Any(x =>
                x.StatusCode == 400 && x.Type == typeof(ApiErrorResponseDTO));
            if (businessError)
            {
                badRequest.Content["application/json"] = new OpenApiMediaType
                {
                    Schema = await context.GetOrCreateSchemaAsync(typeof(ApiErrorResponseDTO), cancellationToken: cancellationToken)
                };
            }
            badRequest.Content["application/problem+json"] = new OpenApiMediaType
            {
                Schema = await context.GetOrCreateSchemaAsync(typeof(ValidationProblemDetails), cancellationToken: cancellationToken)
            };
            badRequest.Description = businessError
                ? "Validación de negocio (application/json) o de binding/JSON (application/problem+json)."
                : "Parámetros inválidos (application/problem+json).";
        }

        operation.Responses ??= new OpenApiResponses();
        operation.Responses["500"] = new OpenApiResponse
        {
            Description = "Error interno con identificador de seguimiento, sin detalles de la excepción.",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/problem+json"] = new()
                {
                    Schema = await context.GetOrCreateSchemaAsync(typeof(ProblemDetails), cancellationToken: cancellationToken)
                }
            }
        };

        if (context.Description.ActionDescriptor is not ControllerActionDescriptor action) return;
        var controller = action.ControllerTypeInfo.AsType();
        var method = action.MethodInfo.Name;
        if (controller == typeof(CoursesController) &&
            method is nameof(CoursesController.GetById) or nameof(CoursesController.Update))
        {
            if (operation.Responses["200"] is OpenApiResponse success)
            {
                success.Headers ??= new Dictionary<string, IOpenApiHeader>();
                success.Headers["ETag"] = new OpenApiHeader
                {
                    Description = "Etiqueta de la versión de trabajo para una edición condicional.",
                    Schema = new OpenApiSchema { Type = JsonSchemaType.String }
                };
            }
            if (method == nameof(CoursesController.Update))
            {
                operation.Parameters ??= [];
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "If-Match",
                    In = ParameterLocation.Header,
                    Description = "Etiqueta obtenida al consultar el curso. Si quedó obsoleta, responde 412.",
                    Schema = new OpenApiSchema { Type = JsonSchemaType.String }
                });
            }
        }
        if (controller == typeof(AuthController) && method == nameof(AuthController.Login) &&
            operation.Responses["429"] is OpenApiResponse rejectedLogin)
        {
            rejectedLogin.Headers ??= new Dictionary<string, IOpenApiHeader>();
            rejectedLogin.Headers["Retry-After"] = new OpenApiHeader
            {
                Description = "Segundos de espera antes de reintentar.",
                Schema = new OpenApiSchema { Type = JsonSchemaType.Integer }
            };
        }

        // Keep nullable arguments and their existing error response; only the document becomes required.
        if (controller == typeof(ReportsController) && method == nameof(ReportsController.GetSummary))
        {
            foreach (var parameter in (operation.Parameters ?? []).OfType<OpenApiParameter>())
            {
                if (parameter.In == ParameterLocation.Query && parameter.Name is "from" or "to")
                {
                    parameter.Required = true;
                }
                if (parameter.In == ParameterLocation.Query && parameter.Name == "timeZone")
                    parameter.Description = "Zona IANA opcional. Con zona, from y to son fechas civiles AAAA-MM-DD.";
            }
        }
    }
}
