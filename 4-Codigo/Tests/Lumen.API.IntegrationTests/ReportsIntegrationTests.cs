using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lumen.DataAccess;
using Lumen.Shared.DTOs;
using Lumen.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.API.IntegrationTests;

public sealed class ReportsIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Theory]
    [InlineData("2026-10-01", "2026-10-05", "2026-09-30T23:59:59.9999999Z", false)]
    [InlineData("2026-10-01", "2026-10-05", "2026-10-01T00:00:00Z", true)]
    [InlineData("2026-10-01", "2026-10-05", "2026-10-03T12:00:00Z", true)]
    [InlineData("2026-10-01", "2026-10-05", "2026-10-05T15:30:00Z", true)]
    [InlineData("2026-10-01", "2026-10-05", "2026-10-06T00:00:00Z", false)]
    [InlineData("2026-10-05", "2026-10-05", "2026-10-05T00:00:00Z", true)]
    [InlineData("2026-10-05", "2026-10-05", "2026-10-05T15:30:00Z", true)]
    [InlineData("2026-10-05", "2026-10-05", "2026-10-05T23:59:59.9999999Z", true)]
    [InlineData("2026-10-05", "2026-10-05", "2026-10-06T15:30:00Z", false)]
    [InlineData("9999-12-31", "9999-12-31", "9999-12-31T23:59:59.9999999Z", true)]
    public async Task Summary_CountsOnlyRecordsWithinSelectedDays(
        string from, string to, string recordedAt, bool included)
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        await SetReportDatesAsync(factory, DateTime.Parse(
            recordedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
        await AuthenticateAsync(client, "admin@lumen.local");

        var response = await client.GetAsync($"/api/reports/summary?from={from}&to={to}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<ReportSummaryResponseDTO>(JsonOptions);
        Assert.NotNull(summary);
        Assert.Equal(included ? 3 : 0, summary.RegisteredUsers);
        Assert.Equal(included ? 2 : 0, summary.CreatedCourses);
        Assert.Equal(included ? 2 : 0, summary.RequestedEnrollments);
        Assert.Equal(DateTime.Parse(from, CultureInfo.InvariantCulture), summary.From);
        Assert.Equal(DateTime.Parse(to, CultureInfo.InvariantCulture), summary.To);
    }

    [Fact]
    public async Task Summary_WhenFromIsDayAfterTo_ReturnsInvalidDateRange()
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, "admin@lumen.local");

        var response = await client.GetAsync("/api/reports/summary?from=2026-10-06&to=2026-10-05");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponseDTO>(JsonOptions);
        Assert.NotNull(error);
        Assert.Equal(nameof(InvalidDateRangeException), error.Code);
    }

    [Fact]
    public async Task Summary_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/reports/summary?from=2026-10-01&to=2026-10-05");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("alumno@lumen.local")]
    [InlineData("profesor@lumen.local")]
    public async Task Summary_WithoutAdministratorRole_ReturnsForbidden(string email)
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, email);

        var response = await client.GetAsync("/api/reports/summary?from=2026-10-01&to=2026-10-05");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task SetReportDatesAsync(LumenApiFactory factory, DateTime recordedAtUtc)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        foreach (var user in await context.Users.ToListAsync())
        {
            user.CreatedAtUtc = recordedAtUtc;
        }
        foreach (var course in await context.Courses.ToListAsync())
        {
            course.CreatedAtUtc = recordedAtUtc;
        }
        foreach (var enrollment in await context.Enrollments.ToListAsync())
        {
            enrollment.RequestedAtUtc = recordedAtUtc;
        }
        await context.SaveChangesAsync();
    }

    private static async Task AuthenticateAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequestDTO(email, DbInitializer.DemoPassword), JsonOptions);
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponseDTO>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
    }
}
