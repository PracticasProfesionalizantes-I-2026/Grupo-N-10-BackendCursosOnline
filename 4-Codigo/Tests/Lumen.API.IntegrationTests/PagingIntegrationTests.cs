using System.Net;
using System.Text.Json;
using Lumen.DataAccess;

namespace Lumen.API.IntegrationTests;

public sealed class PagingIntegrationTests
{
    [Fact]
    public async Task Lists_PageStableFilteredResults_AndKeepTheExistingArrayWhenOmitted()
    {
        using var factory = new LumenApiFactory();
        using var admin = await CourseTestClient.LoginAsync(factory, DemoAccounts.AdministratorEmail);
        using var teacher = await CourseTestClient.LoginAsync(factory, DemoAccounts.TeacherEmail);
        using var student = await CourseTestClient.LoginAsync(factory, DemoAccounts.StudentEmail);
        for (var i = 0; i < 2; i++)
        {
            var draft = await CourseTestClient.DraftAsync(teacher);
            await CourseTestClient.SubmitAsync(teacher, draft.Id);
        }
        var published = await CourseTestClient.PublishedAsync(teacher);
        await CheckAsync(admin, "/api/users");
        await CheckAsync(admin, "/api/course-reviews");
        await CheckAsync(teacher, "/api/course-reviews/mine");
        await CheckAsync(admin, "/api/enrollments?status=Aprobada");
        await CheckAsync(student, "/api/enrollments/mine");
        await CheckAsync(teacher, $"/api/courses/{published.Id}/students/progress");
    }

    private static async Task CheckAsync(HttpClient client, string path)
    {
        var separator = path.Contains('?') ? "&" : "?";
        var full = await ReadAsync(client, path);
        Assert.NotEmpty(full);
        Assert.Equal(full.Take(1), await ReadAsync(client, path + separator + "limit=1"));
        Assert.Equal(full.Skip(1).Take(1), await ReadAsync(client, path + separator + "limit=1&offset=1"));
        Assert.Empty(await ReadAsync(client, path + separator + "limit=100&offset=9999"));
        foreach (var invalid in new[] { "limit=0", "limit=101", "limit=1&offset=-1", "offset=1" })
        {
            using var response = await client.GetAsync(path + separator + invalid);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    private static async Task<string[]> ReadAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.EnumerateArray().Select(x => x.GetRawText()).ToArray();
    }
}
