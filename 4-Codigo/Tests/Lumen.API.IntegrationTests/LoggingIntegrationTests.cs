using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Lumen.DataAccess;
using Lumen.Shared.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using static Lumen.API.IntegrationTests.ConcurrencyTestSupport;

namespace Lumen.API.IntegrationTests;

public sealed class LoggingIntegrationTests
{
    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Production")]
    public async Task DefaultLogging_ExcludesWindowsEventLogAndKeepsConsoleAndDiagnostics(string environment)
    {
        using var root = new LumenApiFactory { PreserveDefaultLogging = true };
        if (environment == "Production")
        {
            // Existing data is prepared by the Testing host, never seeded by Production.
            using var seedClient = root.CreateClient();
        }
        using var factory = root.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    // A filter alone could be overridden by provider-specific configuration.
                    ["Logging:EventLog:LogLevel:Default"] = "Trace",
                    ["Logging:EventLog:LogLevel:Microsoft.EntityFrameworkCore"] = "Trace",
                    ["Logging:Console:LogLevel:Default"] = "Information"
                }));
        });
        using var client = factory.CreateClient();
        var providers = factory.Services.GetServices<ILoggerProvider>().ToArray();
        Assert.DoesNotContain(providers, provider => provider.GetType().FullName ==
            "Microsoft.Extensions.Logging.EventLog.EventLogLoggerProvider");
        Assert.Contains(providers, provider => provider is ConsoleLoggerProvider);
        var logger = factory.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Lumen.LoggingRegression");
        Assert.True(logger.IsEnabled(LogLevel.Warning));
        Assert.True(logger.IsEnabled(LogLevel.Error));
        Assert.True(logger.IsEnabled(LogLevel.Critical));

        // Exercise the real production query paths that emitted the problematic warnings.
        using var login = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDTO("admin@lumen.local", DbInitializer.DemoPassword), Json);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var authenticated = await login.Content.ReadFromJsonAsync<LoginResponseDTO>(Json);
        Assert.NotNull(authenticated);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authenticated.AccessToken);
        using var courses = await client.GetAsync("/api/courses");
        using var enrollments = await client.GetAsync("/api/enrollments");
        Assert.Equal(HttpStatusCode.OK, courses.StatusCode);
        Assert.Equal(HttpStatusCode.OK, enrollments.StatusCode);
        Assert.NotEmpty((await courses.Content.ReadFromJsonAsync<List<CourseResponseDTO>>(Json))!);
        Assert.NotEmpty((await enrollments.Content.ReadFromJsonAsync<List<EnrollmentResponseDTO>>(Json))!);
    }
}
