using System.Net;
using System.Net.Http.Json;
using Lumen.DataAccess;
using Lumen.Shared.DTOs;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Lumen.API.IntegrationTests.ConcurrencyTestSupport;

namespace Lumen.API.IntegrationTests;

public sealed class TraceabilityContractIntegrationTests
{
    [Fact]
    public async Task CourseEdits_RejectAStaleTagAndPreserveTheFirstEdit()
    {
        using var factory = new LumenApiFactory();
        using var teacher = await CourseTestClient.LoginAsync(factory);
        var course = await CourseTestClient.DraftAsync(teacher);
        var path = $"/api/courses/{course.Id}";
        using var original = await teacher.GetAsync(path);
        var tag = original.Headers.ETag!.ToString();
        var update = CourseTestClient.Update(course);

        async Task<HttpResponseMessage> SendAsync(string title, string condition)
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, path)
            {
                Content = JsonContent.Create(update with { Title = title }, options: Json)
            };
            request.Headers.TryAddWithoutValidation("If-Match", condition);
            return await teacher.SendAsync(request);
        }

        using var saved = await SendAsync("Primera edición", tag);
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        Assert.NotEqual(tag, saved.Headers.ETag!.ToString());
        using var stale = await SendAsync("Edición obsoleta", tag);
        Assert.Equal(HttpStatusCode.PreconditionFailed, stale.StatusCode);
        using var weak = await SendAsync("Etiqueta débil", "W/" + saved.Headers.ETag);
        Assert.Equal(HttpStatusCode.PreconditionFailed, weak.StatusCode);
        using var invalid = await SendAsync("Etiqueta inválida", "sin-comillas");
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("Primera edición", (await CourseTestClient.GetAsync(teacher, course.Id)).Title);
        using var refreshed = await SendAsync("Versión actual", $"\"otra-etiqueta\", {saved.Headers.ETag}");
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
    }

    [Theory]
    [InlineData("/api/courses")]
    [InlineData("/api/catalog/courses")]
    public async Task CourseLists_AllowPagingWithoutChangingTheLegacyArray(string path)
    {
        using var factory = new LumenApiFactory();
        using var teacher = await CourseTestClient.LoginAsync(factory);
        await CourseTestClient.DraftAsync(teacher);
        var full = await teacher.GetFromJsonAsync<List<CourseId>>(path, Json);
        Assert.NotNull(full);
        var first = await teacher.GetFromJsonAsync<List<CourseId>>(path + "?limit=1", Json);
        var next = await teacher.GetFromJsonAsync<List<CourseId>>(path + "?limit=1&offset=1", Json);
        Assert.Equal(full.Take(1), first);
        Assert.Equal(full.Skip(1).Take(1), next);
        using var invalid = await teacher.GetAsync(path + "?limit=101");
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using var offsetOnly = await teacher.GetAsync(path + "?offset=1");
        Assert.Equal(HttpStatusCode.BadRequest, offsetOnly.StatusCode);
    }

    [Fact]
    public async Task LoginLimit_UsesConnectionAddressAndReturnsRetryAfter()
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        for (var attempt = 0; attempt <= 20; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
            {
                Content = JsonContent.Create(new LoginRequestDTO("", ""), options: Json)
            };
            request.Headers.Add("X-Forwarded-For", $"192.0.2.{attempt + 1}");
            using var response = await client.SendAsync(request);
            Assert.Equal(attempt < 20 ? HttpStatusCode.BadRequest : HttpStatusCode.TooManyRequests, response.StatusCode);
            if (attempt == 20)
            {
                Assert.InRange(response.Headers.RetryAfter!.Delta!.Value.TotalSeconds, 1, 60);
                Assert.Equal("LoginRateLimitExceeded", (await response.Content.ReadFromJsonAsync<ApiErrorResponseDTO>(Json))!.Code);
            }
        }
    }

    [Fact]
    public async Task Reports_UseTheSelectedCivilDayIncludingItsLastTick()
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, DemoAccounts.AdministratorEmail);
        var start = new DateTime(2024, 3, 10, 5, 0, 0, DateTimeKind.Utc);
        var end = start.AddHours(23); // New York advances its clocks on this date.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
            var users = await db.Users.OrderBy(x => x.Id).ToArrayAsync();
            users[0].CreatedAtUtc = start;
            users[1].CreatedAtUtc = end.AddTicks(-1);
            users[2].CreatedAtUtc = end;
            await db.SaveChangesAsync();
        }
        const string path = "/api/reports/summary?from=2024-03-10&to=2024-03-10";
        var result = await client.GetFromJsonAsync<ReportSummaryResponseDTO>(path + "&timeZone=America%2FNew_York", Json);
        Assert.Equal(2, result!.RegisteredUsers);
        Assert.Equal(start, result.From);
        Assert.Equal(start, result.To);
        Assert.Equal("America/New_York", result.TimeZone);
        var utc = await client.GetFromJsonAsync<ReportSummaryResponseDTO>(path + "&timeZone=UTC", Json);
        Assert.Equal(1, utc!.RegisteredUsers);
        using var invalid = await client.GetAsync(path + "&timeZone=Invalid%2FZone");
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task RevisionOwnershipMigration_RejectsForeignPointersWithoutChangingExistingRows()
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        Assert.Contains("20261105230000_EnforceCourseRevisionOwnership", await db.Database.GetAppliedMigrationsAsync());
        var courses = await db.Courses.AsNoTracking().Where(x => x.PublishedRevisionId.HasValue).OrderBy(x => x.Id).ToArrayAsync();
        var first = courses[0];
        var second = courses[1];
        await Assert.ThrowsAsync<SqliteException>(() => db.Courses.Where(x => x.Id == first.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.PublishedRevisionId, second.PublishedRevisionId)));
        await Assert.ThrowsAsync<SqliteException>(() => db.CourseRevisions.Where(x => x.Id == first.WorkingRevisionId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.CourseId, second.Id)));
        var persisted = await db.Courses.AsNoTracking().SingleAsync(x => x.Id == first.Id);
        Assert.Equal(first.PublishedRevisionId, persisted.PublishedRevisionId);
        Assert.Equal(first.WorkingRevisionId, persisted.WorkingRevisionId);
    }

    private sealed record CourseId(Guid Id);
}
