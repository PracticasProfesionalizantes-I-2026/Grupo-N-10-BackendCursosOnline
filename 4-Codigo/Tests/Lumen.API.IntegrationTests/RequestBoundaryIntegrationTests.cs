using System.Collections.Concurrent;
using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Lumen.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.API.IntegrationTests;

public sealed class RequestBoundaryIntegrationTests
{
    [Fact]
    public async Task OversizeBody_OnKestrelReturns413WithoutWritingOrDisclosingDetails()
    {
        using var factory = new LumenApiFactory();
        factory.UseKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, 0);
            options.Limits.MaxRequestBodySize = 1024;
        });
        using var teacher = await CourseTestClient.LoginAsync(factory);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        var before = await db.Courses.CountAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/courses")
        {
            Content = new ByteArrayContent(new byte[1025])
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Headers.ExpectContinue = true;

        using var response = await teacher.SendAsync(request);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var error = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(413, error.RootElement.GetProperty("status").GetInt32());
        Assert.False(error.RootElement.TryGetProperty("detail", out _));
        Assert.Equal(before, await db.Courses.CountAsync());
    }

    [Fact]
    public async Task CourseManagement_ReadsPublishedAndWorkingModulesWithoutMultiplyingTheRows()
    {
        var queries = new ReadQueries();
        using var factory = new LumenApiFactory(queries);
        using var teacher = await CourseTestClient.LoginAsync(factory);
        var course = await CourseTestClient.PublishedAsync(teacher);
        queries.Commands.Clear();

        var draft = await CourseTestClient.PutAsync(teacher, course.Id,
            CourseTestClient.Update(course) with { Title = "Primer borrador" });
        var updated = await CourseTestClient.PutAsync(teacher, course.Id,
            CourseTestClient.Update(draft) with { Title = "Borrador revisado" });
        var read = await CourseTestClient.GetAsync(teacher, course.Id);

        Assert.Equal(course.Modules.Select(x => x.Id), updated.Modules.Select(x => x.Id));
        Assert.Equal("Borrador revisado", read.Title);
        var moduleQueries = queries.Commands.Where(sql => sql.Contains("\"CourseModules\"")).ToArray();
        Assert.NotEmpty(moduleQueries);
        Assert.All(moduleQueries, sql => Assert.Equal(2, sql.Split("\"CourseModules\"").Length));
    }

    private sealed class ReadQueries : DbCommandInterceptor
    {
        public ConcurrentQueue<string> Commands { get; } = new();

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                Commands.Enqueue(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
