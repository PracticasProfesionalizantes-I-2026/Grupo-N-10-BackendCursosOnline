using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lumen.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.API.IntegrationTests;

public sealed class OpenApiContractTests(OpenApiContractFixture fixture) : IClassFixture<OpenApiContractFixture>
{
    private JsonElement Document => fixture.Document.RootElement;

    public static TheoryData<string, string, string, string, bool, string> Operations => new()
    {
        { "/api/auth/register", "post", "201", "UserResponseDTO", false, "400,409" },
        { "/api/auth/login", "post", "200", "LoginResponseDTO", false, "400,401,403" },
        { "/api/users", "get", "200", "UserResponseDTO", true, "401,403" },
        { "/api/users/{id}", "get", "200", "UserResponseDTO", false, "401,403,404" },
        { "/api/users/administrators", "post", "201", "UserResponseDTO", false, "400,401,403,409" },
        { "/api/catalog/courses", "get", "200", "PublishedCourseResponseDTO", true, "401" },
        { "/api/catalog/courses/{id}", "get", "200", "PublishedCourseResponseDTO", false, "401,404" },
        { "/api/courses", "get", "200", "CourseResponseDTO", true, "401,403" },
        { "/api/courses/{id}", "get", "200", "CourseResponseDTO", false, "401,403,404" },
        { "/api/courses", "post", "201", "CourseResponseDTO", false, "400,401,403" },
        { "/api/courses/{id}", "put", "200", "CourseResponseDTO", false, "400,401,403,404,409" },
        { "/api/courses/{id}/submit-review", "post", "200", "CourseReviewResponseDTO", false, "400,401,403,404,409" },
        { "/api/courses/{id}/pause", "post", "200", "CourseResponseDTO", false, "401,403,404,409" },
        { "/api/courses/{id}/resume", "post", "200", "CourseResponseDTO", false, "401,403,404,409" },
        { "/api/courses/{id}/finish", "post", "200", "CourseResponseDTO", false, "401,403,404,409" },
        { "/api/course-reviews", "get", "200", "CourseReviewResponseDTO", true, "401,403" },
        { "/api/course-reviews/mine", "get", "200", "CourseReviewResponseDTO", true, "401,403" },
        { "/api/course-reviews/{id}/decision", "post", "200", "CourseReviewResponseDTO", false, "400,401,403,404,409" },
        { "/api/enrollments", "post", "201", "EnrollmentResponseDTO", false, "400,401,403,404,409" },
        { "/api/enrollments/mine", "get", "200", "EnrollmentResponseDTO", true, "401,403" },
        { "/api/enrollments", "get", "200", "EnrollmentResponseDTO", true, "400,401,403" },
        { "/api/enrollments/{id}", "get", "200", "EnrollmentResponseDTO", false, "401,403,404" },
        { "/api/enrollments/{id}/course-content", "get", "200", "PublishedCourseResponseDTO", false, "401,403,404,409" },
        { "/api/enrollments/{id}/decision", "post", "200", "EnrollmentResponseDTO", false, "400,401,403,404,409" },
        { "/api/enrollments/{id}/cancel", "post", "200", "EnrollmentResponseDTO", false, "401,403,404,409" },
        { "/api/enrollments/{id}/progress", "get", "200", "ProgressResponseDTO", false, "401,403,404,409" },
        { "/api/enrollments/{id}/modules/{moduleId}/completion", "put", "200", "ProgressResponseDTO", false, "401,403,404,409" },
        { "/api/courses/{courseId}/students/progress", "get", "200", "StudentProgressResponseDTO", true, "401,403,404,409" },
        { "/api/reports/summary", "get", "200", "ReportSummaryResponseDTO", false, "400,401,403" }
    };

    [Theory]
    [MemberData(nameof(Operations))]
    public void Operation_HasItsActualResponseContract(string path, string method, string successCode,
        string dto, bool collection, string errors)
    {
        var responses = Operation(path, method).GetProperty("responses");
        Assert.Equal(errors.Split(',').Append(successCode).Order(), responses.EnumerateObject().Select(x => x.Name).Order());
        var schema = responses.GetProperty(successCode).GetProperty("content").GetProperty("application/json").GetProperty("schema");
        if (collection)
        {
            Assert.Equal("array", schema.GetProperty("type").GetString());
            schema = schema.GetProperty("items");
        }
        Assert.Equal($"#/components/schemas/{dto}", schema.GetProperty("$ref").GetString());
        Assert.Equal("object", Resolve(schema).GetProperty("type").GetString());
        Assert.NotEmpty(Resolve(schema).GetProperty("properties").EnumerateObject());
        foreach (var response in responses.EnumerateObject())
        {
            Assert.False(string.IsNullOrWhiteSpace(response.Value.GetProperty("description").GetString()));
            if (!response.Value.TryGetProperty("content", out var content)) continue;
            Assert.False(content.TryGetProperty("text/plain", out _));
            foreach (var media in content.EnumerateObject())
            {
                var responseSchema = Resolve(media.Value.GetProperty("schema"));
                Assert.NotEqual(JsonValueKind.Undefined, responseSchema.ValueKind);
                if (response.Name is "404" or "409" ||
                    (response.Name is "400" or "401" or "403" && media.Name == "application/json"))
                {
                    AssertBusinessErrorSchema(responseSchema);
                }
            }
        }
    }

    [Fact]
    public void Document_ContainsExactlyThe29ExistingOperations_AndResolvesAllLocalReferences()
    {
        var paths = Document.GetProperty("paths");
        Assert.StartsWith("3.1.", Document.GetProperty("openapi").GetString());
        Assert.Equal(26, paths.EnumerateObject().Count());
        var actual = paths.EnumerateObject().SelectMany(path => path.Value.EnumerateObject()
            .Select(operation => $"{operation.Name} {path.Name}")).Order().ToArray();
        Assert.Equal(29, actual.Length);
        var descriptions = fixture.Factory.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>()
            .ApiDescriptionGroups.Items.SelectMany(x => x.Items).Where(x => x.RelativePath?.StartsWith("api/") == true);
        Assert.Equal(descriptions.Select(x => $"{x.HttpMethod!.ToLowerInvariant()} /{x.RelativePath}").Order(), actual);
        CheckReferences(Document);
    }

    [Fact]
    public void BearerSecurity_MatchesAuthorizationMetadata_ForEveryOperation()
    {
        var scheme = Document.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http", scheme.GetProperty("type").GetString());
        Assert.Equal("bearer", scheme.GetProperty("scheme").GetString());
        Assert.Equal("JWT", scheme.GetProperty("bearerFormat").GetString());
        Assert.False(Document.TryGetProperty("security", out var global) && global.GetArrayLength() > 0);
        var descriptions = fixture.Factory.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>()
            .ApiDescriptionGroups.Items.SelectMany(x => x.Items).Where(x => x.RelativePath?.StartsWith("api/") == true);
        var protectedCount = 0;
        foreach (var description in descriptions)
        {
            var operation = Operation('/' + description.RelativePath!, description.HttpMethod!.ToLowerInvariant());
            var metadata = description.ActionDescriptor.EndpointMetadata;
            var authorize = metadata.OfType<IAuthorizeData>().ToArray();
            var isProtected = authorize.Length > 0 && !metadata.OfType<IAllowAnonymous>().Any();
            var requirements = operation.GetProperty("security").EnumerateArray().ToArray();
            if (!isProtected)
            {
                Assert.Empty(requirements);
                continue;
            }
            protectedCount++;
            Assert.Empty(Assert.Single(requirements).GetProperty("Bearer").EnumerateArray());
            Assert.False(operation.GetProperty("responses").GetProperty("401").TryGetProperty("content", out _));
            foreach (var rule in authorize.Where(x => !string.IsNullOrWhiteSpace(x.Roles)))
            {
                Assert.Contains(rule.Roles!, operation.GetProperty("description").GetString());
                Assert.True(operation.GetProperty("responses").TryGetProperty("403", out _));
            }
        }
        Assert.Equal(27, protectedCount);
    }

    [Fact]
    public void ReportDates_AreRequiredDateTimeQueries_WithoutChangingOtherOptionalQueries()
    {
        var parameters = Operation("/api/reports/summary", "get").GetProperty("parameters").EnumerateArray().ToArray();
        Assert.Equal(new[] { "from", "to" }, parameters.Select(x => x.GetProperty("name").GetString()).Order());
        foreach (var parameter in parameters)
        {
            Assert.Equal("query", parameter.GetProperty("in").GetString());
            Assert.True(parameter.GetProperty("required").GetBoolean());
            Assert.Equal("date-time", Resolve(parameter.GetProperty("schema")).GetProperty("format").GetString());
        }
        var status = Assert.Single(Operation("/api/enrollments", "get").GetProperty("parameters").EnumerateArray());
        Assert.Equal("status", status.GetProperty("name").GetString());
        Assert.False(status.TryGetProperty("required", out var required) && required.GetBoolean());
    }

    [Theory]
    [InlineData("UserRole", "Alumno,Profesor,Administrador")]
    [InlineData("EnrollmentStatus", "Pendiente,Aprobada,Rechazada,Cancelada")]
    [InlineData("ProgressStatus", "NoIniciado,EnProgreso,Completado")]
    [InlineData("CourseReviewType", "Creacion,Modificacion")]
    [InlineData("CourseReviewDecision", "Aprobada,CambiosSolicitados,Rechazada")]
    public void ResponseEnums_UseNamesAndPreserveTheirValues(string type, string values)
    {
        var schema = Document.GetProperty("components").GetProperty("schemas").GetProperty(type);
        Assert.Equal("string", schema.GetProperty("type").GetString());
        Assert.Equal(values.Split(','), schema.GetProperty("enum").EnumerateArray().Select(x => x.GetString()));
        Assert.False(schema.TryGetProperty("anyOf", out _));
    }

    [Theory]
    [InlineData("UserCreateDTO", "role", "Alumno,Profesor", "0,1")]
    [InlineData("EnrollmentDecisionDTO", "decision", "Aprobada,Rechazada", "1,2")]
    [InlineData("CourseReviewDecisionDTO", "decision", "Aprobada,CambiosSolicitados,Rechazada", "0,1,2")]
    public void InputEnums_DescribeBusinessValuesAndExistingNumericCompatibility(string dto, string property,
        string names, string numbers)
    {
        var schema = Document.GetProperty("components").GetProperty("schemas").GetProperty(dto).GetProperty("properties").GetProperty(property);
        Assert.False(schema.TryGetProperty("type", out _)); // A root string constraint would reject the numeric branch.
        Assert.False(schema.TryGetProperty("enum", out _));
        var variants = schema.GetProperty("anyOf").EnumerateArray().ToArray();
        Assert.Equal(3, variants.Length);
        Assert.Equal(names.Split(','), variants[0].GetProperty("enum").EnumerateArray().Select(x => x.GetString()));
        Assert.Equal("integer", variants[1].GetProperty("type").GetString());
        Assert.Equal(numbers.Split(',').Select(int.Parse), variants[1].GetProperty("enum").EnumerateArray().Select(x => x.GetInt32()));
        Assert.Equal(numbers.Split(','), variants[2].GetProperty("enum").EnumerateArray().Select(x => x.GetString()));
    }

    [Fact]
    public void NullableReviewDecision_PreservesNullAndNamedValues()
    {
        var decision = Document.GetProperty("components").GetProperty("schemas")
            .GetProperty("CourseReviewResponseDTO").GetProperty("properties").GetProperty("decision");
        var variants = decision.GetProperty("oneOf").EnumerateArray().ToArray();
        Assert.Equal(2, variants.Length);
        Assert.Contains(variants, x => x.TryGetProperty("type", out var type) && type.GetString() == "null");
        Assert.Contains(variants, x => x.TryGetProperty("$ref", out var reference) &&
            reference.GetString() == "#/components/schemas/CourseReviewDecision");
    }

    [Fact]
    public async Task EnrollmentStatusFilter_PreservesNamedAndNumericQueryValues()
    {
        var parameter = Assert.Single(Operation("/api/enrollments", "get").GetProperty("parameters").EnumerateArray());
        var variants = parameter.GetProperty("schema").GetProperty("anyOf").EnumerateArray().ToArray();
        Assert.Equal(new[] { "Pendiente", "Aprobada", "Rechazada", "Cancelada" },
            variants[0].GetProperty("enum").EnumerateArray().Select(x => x.GetString()));
        Assert.Equal(new[] { 0, 1, 2, 3 }, variants[1].GetProperty("enum").EnumerateArray().Select(x => x.GetInt32()));
        using var client = fixture.Factory.CreateClient();
        await ConcurrencyTestSupport.AuthenticateAsync(client, "admin@lumen.local");
        using var named = await client.GetAsync("/api/enrollments?status=Aprobada");
        using var numeric = await client.GetAsync("/api/enrollments?status=1");
        Assert.Equal(HttpStatusCode.OK, named.StatusCode);
        Assert.Equal(HttpStatusCode.OK, numeric.StatusCode);
        using var namedBody = JsonDocument.Parse(await named.Content.ReadAsStringAsync());
        using var numericBody = JsonDocument.Parse(await numeric.Content.ReadAsStringAsync());
        Assert.NotEmpty(namedBody.RootElement.EnumerateArray());
        Assert.Equal(namedBody.RootElement.GetRawText(), numericBody.RootElement.GetRawText());
        Assert.All(namedBody.RootElement.EnumerateArray(), enrollment =>
            Assert.Equal("Aprobada", enrollment.GetProperty("status").GetString()));
    }

    [Fact]
    public async Task Registration_StillReturns201_StringRoles_AndBusinessErrors()
    {
        using var client = fixture.Factory.CreateClient();
        var user = new { email = $"openapi-{Guid.NewGuid():N}@example.com", password = "Test123!",
            role = "Alumno", firstName = "Test", lastName = "Contrato", dni = "12345678", phone = "123456",
            address = "Calle 1", postalCode = "1000" };
        using var created = await client.PostAsJsonAsync("/api/auth/register", user);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var body = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        Assert.Equal("Alumno", body.RootElement.GetProperty("role").GetString());
        using var duplicate = await client.PostAsJsonAsync("/api/auth/register", user);
        await AssertErrorAsync(duplicate, HttpStatusCode.Conflict, "DuplicateEmailException");
        using var forbiddenRole = await client.PostAsJsonAsync("/api/auth/register", user with { email = $"{Guid.NewGuid():N}@example.com", role = "Administrador" });
        await AssertErrorAsync(forbiddenRole, HttpStatusCode.BadRequest, "PublicRegistrationRoleNotAllowedException");
        using var numeric = await client.PostAsJsonAsync("/api/auth/register", new { email = $"{Guid.NewGuid():N}@example.com", user.password, role = 1,
            user.firstName, user.lastName, user.dni, user.phone, user.address, user.postalCode });
        Assert.Equal(HttpStatusCode.Created, numeric.StatusCode);
        using var numericBody = JsonDocument.Parse(await numeric.Content.ReadAsStringAsync());
        Assert.Equal("Profesor", numericBody.RootElement.GetProperty("role").GetString());
    }

    [Fact]
    public async Task ReportValidation_PreservesBusinessAndBindingErrorContracts()
    {
        using var client = fixture.Factory.CreateClient();
        await ConcurrencyTestSupport.AuthenticateAsync(client, "admin@lumen.local");
        using var missing = await client.GetAsync("/api/reports/summary");
        await AssertErrorAsync(missing, HttpStatusCode.BadRequest, "LumenValidationException");
        using var reversed = await client.GetAsync("/api/reports/summary?from=2026-10-16&to=2026-10-15");
        await AssertErrorAsync(reversed, HttpStatusCode.BadRequest, "InvalidDateRangeException");
        using var malformed = await client.GetAsync("/api/reports/summary?from=invalid&to=2026-10-15");
        Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
        Assert.Equal("application/problem+json", malformed.Content.Headers.ContentType?.MediaType);
        using var invalidBody = JsonDocument.Parse(await malformed.Content.ReadAsStringAsync());
        Assert.True(invalidBody.RootElement.GetProperty("errors").TryGetProperty("from", out _));
        var badRequest = Operation("/api/reports/summary", "get").GetProperty("responses").GetProperty("400").GetProperty("content");
        AssertBusinessErrorSchema(Resolve(badRequest.GetProperty("application/json").GetProperty("schema")));
        Assert.True(Resolve(badRequest.GetProperty("application/problem+json").GetProperty("schema")).GetProperty("properties").TryGetProperty("errors", out _));
        using var valid = await client.GetAsync("/api/reports/summary?from=2026-01-01&to=2026-12-31");
        Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
        Assert.NotNull(await valid.Content.ReadFromJsonAsync<ReportSummaryResponseDTO>());
    }

    [Fact]
    public async Task AuthenticationErrors_DistinguishLoginJsonFromEmptyMiddlewareResponses()
    {
        using var client = fixture.Factory.CreateClient();
        using var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@lumen.local", password = "incorrect" });
        await AssertErrorAsync(login, HttpStatusCode.Unauthorized, "InvalidCredentialsException");
        using var unauthenticated = await client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);
        Assert.Equal(string.Empty, await unauthenticated.Content.ReadAsStringAsync());
        Assert.Contains(unauthenticated.Headers.WwwAuthenticate, x => x.Scheme == "Bearer");
        await ConcurrencyTestSupport.AuthenticateAsync(client, "alumno@lumen.local");
        using var forbidden = await client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(string.Empty, await forbidden.Content.ReadAsStringAsync());
        var login401 = Operation("/api/auth/login", "post").GetProperty("responses").GetProperty("401");
        AssertBusinessErrorSchema(Resolve(login401.GetProperty("content").GetProperty("application/json").GetProperty("schema")));
        Assert.False(Operation("/api/users", "get").GetProperty("responses").GetProperty("403").TryGetProperty("content", out _));
    }

    [Fact]
    public async Task Scalar_AndItsLocalAssets_RemainAvailable()
    {
        using var client = fixture.Factory.CreateClient();
        foreach (var path in new[] { "/scalar/v1", "/scalar/scalar.js", "/scalar/scalar.aspnetcore.js" })
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(string.IsNullOrWhiteSpace(await response.Content.ReadAsStringAsync()));
        }
    }

    private JsonElement Operation(string path, string method) => Document.GetProperty("paths").GetProperty(path).GetProperty(method);
    private JsonElement Resolve(JsonElement schema) => schema.TryGetProperty("$ref", out var reference)
        ? ResolveReference(reference.GetString()!) : schema;
    private JsonElement ResolveReference(string reference)
    {
        Assert.StartsWith("#/", reference);
        return reference[2..].Split('/').Aggregate(Document, (element, part) => element.GetProperty(part.Replace("~1", "/").Replace("~0", "~")));
    }
    private void CheckReferences(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
            foreach (var property in value.EnumerateObject())
            {
                if (property.Name == "$ref") Assert.Equal(JsonValueKind.Object, ResolveReference(property.Value.GetString()!).ValueKind);
                else CheckReferences(property.Value);
            }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var item in value.EnumerateArray()) CheckReferences(item);
    }
    private static void AssertBusinessErrorSchema(JsonElement schema)
    {
        var properties = schema.GetProperty("properties");
        Assert.Equal("string", properties.GetProperty("code").GetProperty("type").GetString());
        Assert.Equal("string", properties.GetProperty("message").GetProperty("type").GetString());
    }
    private static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponseDTO>();
        Assert.Equal(code, body?.Code);
        Assert.False(string.IsNullOrWhiteSpace(body?.Message));
    }
}

public sealed class OpenApiContractFixture : IAsyncLifetime
{
    public LumenApiFactory Factory { get; } = new();
    public JsonDocument Document { get; private set; } = null!;
    public async Task InitializeAsync()
    {
        using var client = Factory.CreateClient();
        using var response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }
    public Task DisposeAsync()
    {
        Document?.Dispose();
        // Use the factory's Dispose override, which also removes its temporary SQLite files.
        Factory.Dispose();
        return Task.CompletedTask;
    }
}
