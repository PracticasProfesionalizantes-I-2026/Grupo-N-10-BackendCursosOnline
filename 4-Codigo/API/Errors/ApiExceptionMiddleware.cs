using Lumen.Shared.DTOs;
using Lumen.Shared.Exceptions;

namespace Lumen.API.Errors;

internal sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted && !context.RequestAborted.IsCancellationRequested)
        {
            context.Response.Clear();
            var status = exception switch
            {
                BadHttpRequestException { StatusCode: StatusCodes.Status413PayloadTooLarge } => StatusCodes.Status413PayloadTooLarge,
                LumenValidationException or PublicRegistrationRoleNotAllowedException or
                    CourseRequiresModuleException or InvalidDateRangeException => StatusCodes.Status400BadRequest,
                InvalidCredentialsException => StatusCodes.Status401Unauthorized,
                InactiveUserException or ForbiddenOperationException or CourseOwnershipException or
                    EnrollmentOwnershipException => StatusCodes.Status403Forbidden,
                UserNotFoundException or CourseNotFoundException or EnrollmentNotFoundException or
                    ModuleNotFoundException or CourseReviewNotFoundException => StatusCodes.Status404NotFound,
                DuplicateEmailException or DuplicateEnrollmentException or CourseStateConflictException or
                    EnrollmentStateConflictException or ModuleCourseMismatchException or
                    DependencyConflictException => StatusCodes.Status409Conflict,
                CourseVersionMismatchException => StatusCodes.Status412PreconditionFailed,
                CourseEditPreconditionRequiredException => StatusCodes.Status428PreconditionRequired,
                _ => StatusCodes.Status500InternalServerError
            };

            if (status == StatusCodes.Status413PayloadTooLarge)
            {
                await Results.Problem(statusCode: status, title: "La solicitud supera el límite de tamaño del servidor.")
                    .ExecuteAsync(context);
                return;
            }

            if (status == StatusCodes.Status500InternalServerError)
            {
                logger.LogError(exception, "Fallo inesperado en la petición {TraceId}", context.TraceIdentifier);
                await Results.Problem(
                    statusCode: status,
                    title: "Ocurrió un error interno.",
                    extensions: new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier })
                    .ExecuteAsync(context);
                return;
            }

            context.Response.StatusCode = status;
            if (status == StatusCodes.Status428PreconditionRequired)
                context.Response.Headers.CacheControl = "no-store";
            await context.Response.WriteAsJsonAsync(
                new ApiErrorResponseDTO(exception.GetType().Name, exception.Message), context.RequestAborted);
        }
    }
}
