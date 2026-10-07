using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lumen.DataAccess;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;

namespace Lumen.API.IntegrationTests;

public sealed class CourseCreationIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task SubmitReview_RejectsModuleWithoutContentOrResources_AndKeepsDraft()
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        await AuthenticateProfessorAsync(client);
        var course = await CreateDraftAsync(client,
        [new CourseModuleCreateDTO("Primero", "Descripción", 60, "Contenido", []),
         new CourseModuleCreateDTO("Segundo", "Descripción", 30, " ", [" "])]);

        Assert.Equal("Borrador", course.Status);
        Assert.Equal(90, course.TotalDurationMinutes);
        Assert.Empty(course.Modules[1].Resources);

        var response = await client.PostAsync($"/api/courses/{course.Id}/submit-review", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponseDTO>(JsonOptions);
        Assert.Equal("LumenValidationException", error?.Code);
        var saved = await client.GetFromJsonAsync<CourseResponseDTO>($"/api/courses/{course.Id}", JsonOptions);
        Assert.Equal("Borrador", saved?.Status);
        var reviews = await client.GetFromJsonAsync<List<CourseReviewResponseDTO>>("/api/course-reviews/mine", JsonOptions);
        Assert.DoesNotContain(reviews!, review => review.CourseId == course.Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SubmitReview_AcceptsModuleWithContentOrResources(bool resourcesOnly)
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        await AuthenticateProfessorAsync(client);
        var module = new CourseModuleCreateDTO("Módulo", "Descripción", 60,
            resourcesOnly ? "" : "Contenido", resourcesOnly ? ["guia.pdf"] : []);
        var course = await CreateDraftAsync(client, [module]);

        var response = await client.PostAsync($"/api/courses/{course.Id}/submit-review", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var review = await response.Content.ReadFromJsonAsync<CourseReviewResponseDTO>(JsonOptions);
        Assert.Equal(CourseReviewType.Creacion, review?.Type);
        Assert.Equal(course.Id, review?.CourseId);
        var saved = await client.GetFromJsonAsync<CourseResponseDTO>($"/api/courses/{course.Id}", JsonOptions);
        Assert.Equal("EnRevision", saved?.Status);
        Assert.Equal(60, saved?.TotalDurationMinutes);
        var reviews = await client.GetFromJsonAsync<List<CourseReviewResponseDTO>>("/api/course-reviews/mine", JsonOptions);
        Assert.Contains(reviews!, item => item.Id == review!.Id && item.CourseId == course.Id);
    }

    private static async Task<CourseResponseDTO> CreateDraftAsync(
        HttpClient client, IReadOnlyList<CourseModuleCreateDTO> modules)
    {
        var dto = new CourseCreateDTO("Curso", "Descripción", "Categoría", "Inicial", "Online", 20,
            ["Aprender"], ["Ninguno"], modules);
        var response = await client.PostAsJsonAsync("/api/courses", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CourseResponseDTO>(JsonOptions))!;
    }

    private static async Task AuthenticateProfessorAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDTO("profesor@lumen.local", DbInitializer.DemoPassword), JsonOptions);
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponseDTO>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
    }
}
