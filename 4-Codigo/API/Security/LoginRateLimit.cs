using System.Globalization;
using System.Threading.RateLimiting;
using Lumen.Shared.DTOs;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;

namespace Lumen.API.Security;

internal static class LoginRateLimit
{
    internal const string Policy = "login";
    private const int Attempts = 20;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    internal static void Configure(RateLimiterOptions options)
    {
        options.AddPolicy(Policy, context =>
        {
            var address = context.Connection.RemoteIpAddress;
            if (address?.IsIPv4MappedToIPv6 == true) address = address.MapToIPv4();
            return RateLimitPartition.GetSlidingWindowLimiter(address?.ToString() ?? "unknown", _ =>
                new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = Attempts,
                    Window = Window,
                    SegmentsPerWindow = 6,
                    QueueLimit = 0
                });
        });
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, cancellationToken) =>
        {
            var wait = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry) ? retry : Window;
            context.HttpContext.Response.Headers[HeaderNames.RetryAfter] =
                Math.Max(1, (int)Math.Ceiling(wait.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
            await context.HttpContext.Response.WriteAsJsonAsync(
                new ApiErrorResponseDTO("LoginRateLimitExceeded", "Demasiados intentos. Volvé a intentar más tarde."),
                cancellationToken);
        };
    }
}
