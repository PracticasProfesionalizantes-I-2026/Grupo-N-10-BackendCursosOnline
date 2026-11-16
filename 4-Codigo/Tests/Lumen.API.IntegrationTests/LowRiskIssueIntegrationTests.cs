using System.Net;
using System.Net.Http.Json;
using Lumen.DataAccess;
using Lumen.DataAccess.Entities;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Lumen.API.IntegrationTests;

public sealed class LowRiskIssueIntegrationTests
{
    [Theory]
    [InlineData(1, HttpStatusCode.Created)]
    [InlineData(9_999, HttpStatusCode.Created)]
    [InlineData(10_000, HttpStatusCode.Created)]
    [InlineData(0, HttpStatusCode.BadRequest)]
    [InlineData(-1, HttpStatusCode.BadRequest)]
    [InlineData(10_001, HttpStatusCode.BadRequest)]
    public async Task Capacity_CreateHonorsBusinessBounds(int capacity, HttpStatusCode expected)
    {
        using var factory = new LumenApiFactory();
        using var client = await CourseTestClient.LoginAsync(factory);
        var dto = new CourseCreateDTO("Curso", "Descripción", "Categoría", "Inicial", "Online", capacity,
            [], [], [new("Módulo", "Descripción", 30, "Contenido", [])]);
        using var response = await client.PostAsJsonAsync("/api/courses", dto, CourseTestClient.Json);
        Assert.Equal(expected, response.StatusCode);
        if (expected == HttpStatusCode.Created)
            Assert.Equal(capacity, (await response.Content.ReadFromJsonAsync<CourseResponseDTO>(CourseTestClient.Json))!.MaxCapacity);
        else
            Assert.Equal("LumenValidationException",
                (await response.Content.ReadFromJsonAsync<ApiErrorResponseDTO>(CourseTestClient.Json))!.Code);
    }

    [Fact]
    public async Task Capacity_EditAndNumericETag_RejectStaleVersionAndAllowRetry()
    {
        using var factory = new LumenApiFactory();
        using var client = await CourseTestClient.LoginAsync(factory);
        var draft = await CourseTestClient.DraftAsync(client);
        var initialTag = await CourseTestClient.ReadTagAsync(client, draft.Id);
        Assert.EndsWith(".0000000000000001\"", initialTag, StringComparison.Ordinal);

        using var accepted = await CourseTestClient.SendEditAsync(client, draft.Id,
            CourseTestClient.Update(draft) with { MaxCapacity = 10_000 }, initialTag);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        var currentTag = accepted.Headers.ETag!.ToString();
        Assert.EndsWith(".0000000000000002\"", currentTag, StringComparison.Ordinal);
        using var stale = await CourseTestClient.SendEditAsync(client, draft.Id,
            CourseTestClient.Update(draft) with { MaxCapacity = 9_999 }, initialTag);
        Assert.Equal(HttpStatusCode.PreconditionFailed, stale.StatusCode);
        using var tooLarge = await CourseTestClient.SendEditAsync(client, draft.Id,
            CourseTestClient.Update(draft) with { MaxCapacity = 10_001 }, currentTag);
        Assert.Equal(HttpStatusCode.BadRequest, tooLarge.StatusCode);
        Assert.Equal(currentTag, await CourseTestClient.ReadTagAsync(client, draft.Id));
        using var retry = await CourseTestClient.SendEditAsync(client, draft.Id,
            CourseTestClient.Update(draft) with { MaxCapacity = 9_999 }, currentTag);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.EndsWith(".0000000000000003\"", retry.Headers.ETag!.ToString(), StringComparison.Ordinal);
        Assert.Equal(9_999, (await CourseTestClient.GetAsync(client, draft.Id)).MaxCapacity);
    }

    [Fact]
    public async Task Migration_AddsNumericVersionToExistingCourseWithoutChangingHistory()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new LumenDbContext(new DbContextOptionsBuilder<LumenDbContext>()
            .UseSqlite(connection, sqlite => sqlite.MigrationsAssembly("Lumen.Migrations")).Options);
        await db.GetService<IMigrator>().MigrateAsync("20261106230000_EnforceCourseCapacity");
        var user = new User { Id = Guid.NewGuid(), Email = "legacy@example.test", NormalizedEmail = "LEGACY@EXAMPLE.TEST", Role = UserRole.Profesor };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var id = Guid.NewGuid();
        var oldStamp = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Courses" ("Id", "CreatedByUserId", "OperationalStatus", "CreatedAtUtc", "UpdatedAtUtc")
            VALUES ({id}, {user.Id}, {"Activo"}, {oldStamp}, {oldStamp})
            """);

        await db.Database.MigrateAsync();

        var course = await db.Courses.SingleAsync(x => x.Id == id);
        Assert.Equal(1, course.RowVersion);
        Assert.Equal(oldStamp, course.UpdatedAtUtc);
        Assert.Contains("20261116200347_AddCourseRowVersion", await db.Database.GetAppliedMigrationsAsync());
        course.RowVersion++;
        await db.SaveChangesAsync();
        Assert.Equal(2, (await db.Courses.AsNoTracking().SingleAsync(x => x.Id == id)).RowVersion);
        await using var integrity = connection.CreateCommand();
        integrity.CommandText = "PRAGMA integrity_check";
        Assert.Equal("ok", (string?)await integrity.ExecuteScalarAsync());
    }
}
