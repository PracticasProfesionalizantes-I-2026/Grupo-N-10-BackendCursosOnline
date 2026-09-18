using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lumen.DataAccess;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;

namespace Lumen.API.IntegrationTests;

public sealed class ApiIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task OpenApiAndScalar_AreAvailable()
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();

        var openApi = await client.GetAsync("/openapi/v1.json");
        var scalar = await client.GetAsync("/scalar/v1");

        Assert.Equal(HttpStatusCode.OK, openApi.StatusCode);
        Assert.Equal(HttpStatusCode.OK, scalar.StatusCode);
    }

    [Fact]
    public async Task Catalog_RequiresAuthenticationAndReturnsPublishedCourses()
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/catalog/courses")).StatusCode);

        await AuthenticateAsync(client, "alumno@lumen.local");
        var courses = await client.GetFromJsonAsync<List<PublishedCourseResponseDTO>>("/api/catalog/courses", JsonOptions);

        Assert.NotNull(courses);
        Assert.Equal(2, courses.Count);
    }

    [Fact]
    public async Task SeededStudent_CanReadApprovedEnrollmentProgress()
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, "alumno@lumen.local");

        var enrollments = await client.GetFromJsonAsync<List<EnrollmentResponseDTO>>("/api/enrollments/mine", JsonOptions);
        var approved = Assert.Single(enrollments!, x => x.Status == EnrollmentStatus.Aprobada);
        var progress = await client.GetFromJsonAsync<ProgressResponseDTO>(
            $"/api/enrollments/{approved.Id}/progress", JsonOptions);

        Assert.NotNull(progress);
        Assert.Equal(ProgressStatus.EnProgreso, progress.Status);
        Assert.True(progress.Percentage > 0);
    }

    [Fact]
    public async Task PublishedCourseUpdate_KeepsOldVersionPublicUntilApproval()
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, "profesor@lumen.local");
        var manageable = await client.GetFromJsonAsync<List<CourseResponseDTO>>("/api/courses", JsonOptions);
        var course = manageable!.First(x => x.PublishedVersion.HasValue);
        var previousPublishedTitle = course.Title;
        var updatedTitle = $"{course.Title} v{course.WorkingVersion + 1}";
        var update = new CourseUpdateDTO(
            updatedTitle,
            course.Description,
            course.Category,
            course.Level,
            course.Modality,
            course.MaxCapacity,
            course.LearningObjectives,
            course.SuggestedPrerequisites,
            course.Modules.Select(x => new CourseModuleUpdateDTO(
                x.Id, x.Name, x.Description, x.DurationMinutes, x.Content ?? string.Empty, x.Resources)).ToList());

        var updateResponse = await client.PutAsJsonAsync($"/api/courses/{course.Id}", update, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var submitResponse = await client.PostAsync($"/api/courses/{course.Id}/submit-review", null);
        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);

        var catalogDuringReview = await client.GetFromJsonAsync<List<PublishedCourseResponseDTO>>("/api/catalog/courses", JsonOptions);
        Assert.Contains(catalogDuringReview!, x => x.Id == course.Id && x.Title == previousPublishedTitle);
        Assert.DoesNotContain(catalogDuringReview!, x => x.Id == course.Id && x.Title == updatedTitle);

        await AuthenticateAsync(client, "admin@lumen.local");
        var reviews = await client.GetFromJsonAsync<List<CourseReviewResponseDTO>>("/api/course-reviews", JsonOptions);
        var review = Assert.Single(reviews!, x => x.CourseId == course.Id);
        var decisionResponse = await client.PostAsJsonAsync(
            $"/api/course-reviews/{review.Id}/decision",
            new CourseReviewDecisionDTO(CourseReviewDecision.Aprobada, null),
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, decisionResponse.StatusCode);

        var catalogAfterApproval = await client.GetFromJsonAsync<List<PublishedCourseResponseDTO>>("/api/catalog/courses", JsonOptions);
        Assert.Contains(catalogAfterApproval!, x =>
            x.Id == course.Id && x.Title == updatedTitle && x.Version == course.WorkingVersion + 1);
    }

    [Fact]
    public async Task Reports_RejectInvalidRangeAndReturnSeedCounts()
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, "admin@lumen.local");

        var invalid = await client.GetAsync("/api/reports/summary?from=2026-02-01&to=2026-01-01");
        var valid = await client.GetFromJsonAsync<ReportSummaryResponseDTO>(
            "/api/reports/summary?from=2020-01-01&to=2030-01-01", JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.NotNull(valid);
        Assert.Equal(3, valid.RegisteredUsers);
        Assert.Equal(2, valid.CreatedCourses);
        Assert.Equal(2, valid.RequestedEnrollments);
    }

    private static async Task AuthenticateAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDTO(email, DbInitializer.DemoPassword),
            JsonOptions);
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponseDTO>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
    }
}
