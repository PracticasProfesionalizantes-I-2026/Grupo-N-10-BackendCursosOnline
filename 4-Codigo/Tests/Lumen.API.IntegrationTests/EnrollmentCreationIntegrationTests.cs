using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lumen.DataAccess;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Lumen.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Lumen.API.IntegrationTests;

public sealed class EnrollmentCreationIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task Create_PersistsOnlyEnrollmentUsesJwtIdentityAndReturnsReadableLocation()
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        var student = await RegisterStudentAsync(client);
        var course = await FirstCourseAsync(client);
        var graphBefore = await CourseGraphAsync(factory);
        var countBefore = await EnrollmentCountAsync(factory);
        var before = DateTime.UtcNow;

        var response = await client.PostAsJsonAsync("/api/enrollments", new
        {
            courseId = course.Id, studentId = Guid.NewGuid(), status = "Aprobada"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var enrollment = (await response.Content.ReadFromJsonAsync<EnrollmentResponseDTO>(JsonOptions))!;
        Assert.NotEqual(Guid.Empty, enrollment.Id);
        Assert.Equal(student.Id, enrollment.StudentId);
        Assert.Equal(course.Id, enrollment.CourseId);
        Assert.Equal(course.Title, enrollment.CourseTitle);
        Assert.Equal(EnrollmentStatus.Pendiente, enrollment.Status);
        Assert.InRange(enrollment.RequestedAtUtc, before, DateTime.UtcNow);
        Assert.Null(enrollment.ResolvedAtUtc);
        Assert.Null(enrollment.CanceledAtUtc);
        Assert.NotNull(response.Headers.Location);
        var detail = await client.GetFromJsonAsync<EnrollmentResponseDTO>(response.Headers.Location, JsonOptions);
        Assert.Equal(enrollment, detail);
        var mine = await client.GetFromJsonAsync<List<EnrollmentResponseDTO>>("/api/enrollments/mine", JsonOptions);
        Assert.Equal(enrollment, Assert.Single(mine!));
        Assert.Equal(countBefore + 1, await EnrollmentCountAsync(factory));
        Assert.Equal(graphBefore, await CourseGraphAsync(factory));

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        Assert.StartsWith(Path.GetTempPath(), context.Database.GetDbConnection().DataSource, StringComparison.OrdinalIgnoreCase);
        var persisted = await context.Enrollments.AsNoTracking().SingleAsync(x => x.Id == enrollment.Id);
        Assert.Equal(student.Id, persisted.StudentId);
        Assert.Equal(EnrollmentStatus.Pendiente, persisted.Status);
        Assert.Equal(enrollment.RequestedAtUtc, persisted.RequestedAtUtc);
        Assert.Null(persisted.ResolvedByUserId);
        Assert.Null(persisted.CanceledByUserId);
        Assert.False(await context.EnrollmentModuleCompletions.AnyAsync(x => x.EnrollmentId == enrollment.Id));
        await AssertErrorAsync(await client.GetAsync($"/api/enrollments/{enrollment.Id}/course-content"),
            HttpStatusCode.Conflict, nameof(EnrollmentStateConflictException));
    }

    [Fact]
    public async Task Create_WithoutAuthenticationReturns401WithoutInserting()
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        var before = await EnrollmentCountAsync(factory);
        var response = await client.PostAsJsonAsync("/api/enrollments", new EnrollmentCreateDTO(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(before, await EnrollmentCountAsync(factory));
    }

    [Theory]
    [InlineData("profesor@lumen.local")]
    [InlineData("admin@lumen.local")]
    public async Task Create_OtherRolesReturn403WithoutInserting(string email)
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, email);
        var course = await FirstCourseAsync(client);
        var before = await EnrollmentCountAsync(factory);

        var response = await client.PostAsJsonAsync("/api/enrollments", new EnrollmentCreateDTO(course.Id));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(before, await EnrollmentCountAsync(factory));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"courseId\":\"00000000-0000-0000-0000-000000000000\"}")]
    [InlineData("{\"courseId\":\"invalid-guid\"}")]
    [InlineData("{\"courseId\":null}")]
    [InlineData("{\"courseId\":123}")]
    [InlineData("{")]
    [InlineData("null")]
    public async Task Create_InvalidBodyReturns400WithoutInserting(string json)
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, "alumno@lumen.local");
        var before = await EnrollmentCountAsync(factory);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/enrollments", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, await EnrollmentCountAsync(factory));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Create_UnusableSignedStudentIdentityReturns403WithoutInserting(string? identity)
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        var claims = new List<Claim> { new(ClaimTypes.Role, "Alumno") };
        if (identity is not null) claims.Add(new Claim(ClaimTypes.NameIdentifier, identity));
        var token = new JwtSecurityToken(configuration["Jwt:Issuer"], configuration["Jwt:Audience"], claims,
            expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)), SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        var before = await EnrollmentCountAsync(factory);

        var response = await client.PostAsJsonAsync("/api/enrollments", new EnrollmentCreateDTO(Guid.NewGuid()));

        await AssertErrorAsync(response, HttpStatusCode.Forbidden, nameof(ForbiddenOperationException));
        Assert.Equal(before, await EnrollmentCountAsync(factory));
    }

    [Fact]
    public async Task Create_MissingCourseReturns404WithoutInserting()
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, "alumno@lumen.local");
        var before = await EnrollmentCountAsync(factory);

        var response = await client.PostAsJsonAsync("/api/enrollments", new EnrollmentCreateDTO(Guid.NewGuid()));

        await AssertErrorAsync(response, HttpStatusCode.NotFound, nameof(CourseNotFoundException));
        Assert.Equal(before, await EnrollmentCountAsync(factory));
    }

    [Theory]
    [InlineData(CourseOperationalStatus.Pausado, true)]
    [InlineData(CourseOperationalStatus.Finalizado, true)]
    [InlineData(CourseOperationalStatus.Activo, false)]
    public async Task Create_UnavailableCourseReturns409WithoutInserting(CourseOperationalStatus status, bool published)
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        await RegisterStudentAsync(client);
        var course = await FirstCourseAsync(client);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
            var entity = await context.Courses.SingleAsync(x => x.Id == course.Id);
            entity.OperationalStatus = status;
            if (!published) entity.PublishedRevisionId = null;
            await context.SaveChangesAsync();
        }
        var before = await EnrollmentCountAsync(factory);

        var response = await client.PostAsJsonAsync("/api/enrollments", new EnrollmentCreateDTO(course.Id));

        await AssertErrorAsync(response, HttpStatusCode.Conflict, nameof(CourseStateConflictException));
        Assert.Equal(before, await EnrollmentCountAsync(factory));
    }

    [Theory]
    [InlineData(EnrollmentStatus.Pendiente)]
    [InlineData(EnrollmentStatus.Aprobada)]
    public async Task Create_ExistingPendingOrApprovedReturns409WithoutInserting(EnrollmentStatus status)
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, "alumno@lumen.local");
        var mine = await client.GetFromJsonAsync<List<EnrollmentResponseDTO>>("/api/enrollments/mine", JsonOptions);
        var existing = Assert.Single(mine!, x => x.Status == status);
        var before = await EnrollmentCountAsync(factory);

        var response = await client.PostAsJsonAsync("/api/enrollments", new EnrollmentCreateDTO(existing.CourseId));

        await AssertErrorAsync(response, HttpStatusCode.Conflict, nameof(DuplicateEnrollmentException));
        Assert.Equal(before, await EnrollmentCountAsync(factory));
    }

    [Fact]
    public async Task Create_ConcurrentHttpRequestsCreateExactlyOnePendingEnrollment()
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        var student = await RegisterStudentAsync(client);
        var course = await FirstCourseAsync(client);

        var responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ =>
            client.PostAsJsonAsync("/api/enrollments", new EnrollmentCreateDTO(course.Id))));

        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Created);
        foreach (var response in responses.Where(x => x.StatusCode != HttpStatusCode.Created))
            await AssertErrorAsync(response, HttpStatusCode.Conflict, nameof(DuplicateEnrollmentException));
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        var persisted = Assert.Single(await context.Enrollments.Where(x => x.StudentId == student.Id && x.CourseId == course.Id).ToListAsync());
        Assert.Equal(EnrollmentStatus.Pendiente, persisted.Status);
    }

    [Theory]
    [InlineData("pause")]
    [InlineData("finish")]
    public async Task Create_WhenCourseChangesAfterServiceCheckReturns409WithoutInserting(string operation)
    {
        var gate = new EnrollmentInsertGate();
        using var factory = new LumenApiFactory(gate);
        using var studentClient = factory.CreateClient();
        await RegisterStudentAsync(studentClient);
        var course = await FirstCourseAsync(studentClient);
        using var adminClient = factory.CreateClient();
        await AuthenticateAsync(adminClient, "admin@lumen.local");
        var before = await EnrollmentCountAsync(factory);
        gate.Arm();
        var request = studentClient.PostAsJsonAsync("/api/enrollments", new EnrollmentCreateDTO(course.Id));
        try
        {
            await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
            var change = await adminClient.PostAsync($"/api/courses/{course.Id}/{operation}", null);
            Assert.Equal(HttpStatusCode.OK, change.StatusCode);
        }
        finally { gate.Release.TrySetResult(); }

        await AssertErrorAsync(await request, HttpStatusCode.Conflict, nameof(CourseStateConflictException));
        Assert.Equal(before, await EnrollmentCountAsync(factory));
    }

    [Fact]
    public async Task Create_DuringNewReviewUsesExistingPublishedVersion()
    {
        using var factory = new LumenApiFactory();
        using var teacher = factory.CreateClient();
        await AuthenticateAsync(teacher, "profesor@lumen.local");
        var courses = await teacher.GetFromJsonAsync<List<CourseResponseDTO>>("/api/courses", JsonOptions);
        var course = courses!.First(x => x.PublishedVersion.HasValue);
        var publishedTitle = course.Title;
        var update = new CourseUpdateDTO("Cambios sin aprobar", course.Description, course.Category, course.Level,
            course.Modality, course.MaxCapacity, course.LearningObjectives, course.SuggestedPrerequisites,
            course.Modules.Select(x => new CourseModuleUpdateDTO(x.Id, x.Name, x.Description, x.DurationMinutes, x.Content!, x.Resources)).ToList());
        Assert.Equal(HttpStatusCode.OK, (await teacher.PutAsJsonAsync($"/api/courses/{course.Id}", update, JsonOptions)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await teacher.PostAsync($"/api/courses/{course.Id}/submit-review", null)).StatusCode);
        using var student = factory.CreateClient();
        await RegisterStudentAsync(student);

        var response = await student.PostAsJsonAsync("/api/enrollments", new EnrollmentCreateDTO(course.Id));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var enrollment = (await response.Content.ReadFromJsonAsync<EnrollmentResponseDTO>(JsonOptions))!;
        Assert.Equal(publishedTitle, enrollment.CourseTitle);
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        var current = await context.Courses.Include(x => x.WorkingRevision).SingleAsync(x => x.Id == course.Id);
        Assert.Equal(CourseRevisionStatus.EnRevision, current.WorkingRevision!.Status);
        Assert.NotEqual(current.WorkingRevisionId, current.PublishedRevisionId);
    }

    [Fact]
    public async Task Create_AfterRejectionPreservesRejectedRequestAndNeedsNewAdministrativeDecision()
    {
        using var factory = new LumenApiFactory();
        using var student = factory.CreateClient();
        await AuthenticateAsync(student, "alumno@lumen.local");
        var mine = await student.GetFromJsonAsync<List<EnrollmentResponseDTO>>("/api/enrollments/mine", JsonOptions);
        var original = Assert.Single(mine!, x => x.Status == EnrollmentStatus.Pendiente);
        using var admin = factory.CreateClient();
        await AuthenticateAsync(admin, "admin@lumen.local");
        var rejection = await admin.PostAsJsonAsync($"/api/enrollments/{original.Id}/decision",
            new EnrollmentDecisionDTO(EnrollmentStatus.Rechazada), JsonOptions);
        Assert.Equal(HttpStatusCode.OK, rejection.StatusCode);
        var rejected = (await rejection.Content.ReadFromJsonAsync<EnrollmentResponseDTO>(JsonOptions))!;

        var response = await student.PostAsJsonAsync("/api/enrollments", new EnrollmentCreateDTO(original.CourseId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var replacement = (await response.Content.ReadFromJsonAsync<EnrollmentResponseDTO>(JsonOptions))!;
        Assert.NotEqual(original.Id, replacement.Id);
        Assert.Equal(EnrollmentStatus.Pendiente, replacement.Status);
        Assert.Equal(rejected, await student.GetFromJsonAsync<EnrollmentResponseDTO>($"/api/enrollments/{original.Id}", JsonOptions));
        await AssertErrorAsync(await student.GetAsync($"/api/enrollments/{replacement.Id}/course-content"),
            HttpStatusCode.Conflict, nameof(EnrollmentStateConflictException));
        var pending = await admin.GetFromJsonAsync<List<EnrollmentResponseDTO>>("/api/enrollments?status=Pendiente", JsonOptions);
        Assert.Contains(pending!, x => x.Id == replacement.Id);
        Assert.DoesNotContain(pending!, x => x.Id == original.Id);
    }

    [Fact]
    public async Task Create_ApprovalCancellationAndNewRequestKeepHistoryAndSeparateProgress()
    {
        using var factory = new LumenApiFactory();
        using var student = factory.CreateClient();
        await RegisterStudentAsync(student);
        var course = await FirstCourseAsync(student);
        var first = await CreateAsync(student, course.Id);
        using var admin = factory.CreateClient();
        await AuthenticateAsync(admin, "admin@lumen.local");
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/enrollments/{first.Id}/decision",
            new EnrollmentDecisionDTO(EnrollmentStatus.Aprobada), JsonOptions)).StatusCode);
        var content = await student.GetFromJsonAsync<PublishedCourseResponseDTO>($"/api/enrollments/{first.Id}/course-content", JsonOptions);
        Assert.Equal(HttpStatusCode.OK, (await student.PutAsync($"/api/enrollments/{first.Id}/modules/{content!.Modules[0].Id}/completion", null)).StatusCode);
        var cancel = await student.PostAsync($"/api/enrollments/{first.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        var canceled = (await cancel.Content.ReadFromJsonAsync<EnrollmentResponseDTO>(JsonOptions))!;

        var replacement = await CreateAsync(student, course.Id);

        Assert.NotEqual(first.Id, replacement.Id);
        Assert.Equal(EnrollmentStatus.Pendiente, replacement.Status);
        Assert.Equal(canceled, await student.GetFromJsonAsync<EnrollmentResponseDTO>($"/api/enrollments/{first.Id}", JsonOptions));
        foreach (var id in new[] { first.Id, replacement.Id })
            await AssertErrorAsync(await student.GetAsync($"/api/enrollments/{id}/course-content"),
                HttpStatusCode.Conflict, nameof(EnrollmentStateConflictException));
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/enrollments/{replacement.Id}/decision",
            new EnrollmentDecisionDTO(EnrollmentStatus.Aprobada), JsonOptions)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await student.GetAsync($"/api/enrollments/{replacement.Id}/course-content")).StatusCode);
        var progress = await student.GetFromJsonAsync<ProgressResponseDTO>($"/api/enrollments/{replacement.Id}/progress", JsonOptions);
        Assert.Equal(0, progress!.Percentage);
        Assert.Equal(ProgressStatus.NoIniciado, progress.Status);
        await AssertErrorAsync(await student.GetAsync($"/api/enrollments/{first.Id}/progress"),
            HttpStatusCode.Conflict, nameof(EnrollmentStateConflictException));
        await AssertErrorAsync(await admin.PostAsJsonAsync($"/api/enrollments/{replacement.Id}/decision",
            new EnrollmentDecisionDTO(EnrollmentStatus.Rechazada), JsonOptions),
            HttpStatusCode.Conflict, nameof(EnrollmentStateConflictException));
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        Assert.Equal(1, await context.EnrollmentModuleCompletions.CountAsync(x => x.EnrollmentId == first.Id));
        Assert.False(await context.EnrollmentModuleCompletions.AnyAsync(x => x.EnrollmentId == replacement.Id));
    }

    [Fact]
    public async Task CreatedEnrollment_RemainsPrivateToItsStudent()
    {
        using var factory = new LumenApiFactory();
        using var owner = factory.CreateClient();
        await RegisterStudentAsync(owner);
        var enrollment = await CreateAsync(owner, (await FirstCourseAsync(owner)).Id);
        using var other = factory.CreateClient();
        await RegisterStudentAsync(other);

        await AssertErrorAsync(await other.GetAsync($"/api/enrollments/{enrollment.Id}"),
            HttpStatusCode.Forbidden, nameof(EnrollmentOwnershipException));
        var mine = await other.GetFromJsonAsync<List<EnrollmentResponseDTO>>("/api/enrollments/mine", JsonOptions);
        Assert.Empty(mine!);
    }

    private static async Task<UserResponseDTO> RegisterStudentAsync(HttpClient client)
    {
        var email = $"cu06-{Guid.NewGuid():N}@lumen.local";
        var registration = await client.PostAsJsonAsync("/api/auth/register", new UserCreateDTO(
            email, DbInitializer.DemoPassword, UserRole.Alumno, "Alumno", "CU06", "12345678", "123456", "Dirección", "1000"), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        await AuthenticateAsync(client, email);
        return (await registration.Content.ReadFromJsonAsync<UserResponseDTO>(JsonOptions))!;
    }

    private static async Task AuthenticateAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO(email, DbInitializer.DemoPassword), JsonOptions);
        response.EnsureSuccessStatusCode();
        var login = (await response.Content.ReadFromJsonAsync<LoginResponseDTO>(JsonOptions))!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
    }

    private static async Task<PublishedCourseResponseDTO> FirstCourseAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<List<PublishedCourseResponseDTO>>("/api/catalog/courses", JsonOptions))![0];

    private static async Task<EnrollmentResponseDTO> CreateAsync(HttpClient client, Guid courseId)
    {
        var response = await client.PostAsJsonAsync("/api/enrollments", new EnrollmentCreateDTO(courseId));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<EnrollmentResponseDTO>(JsonOptions))!;
    }

    private static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        var error = (await response.Content.ReadFromJsonAsync<ApiErrorResponseDTO>(JsonOptions))!;
        Assert.Equal(code, error.Code);
    }

    private static async Task<int> EnrollmentCountAsync(LumenApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<LumenDbContext>().Enrollments.CountAsync();
    }

    private static async Task<string> CourseGraphAsync(LumenApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        return JsonSerializer.Serialize(new
        {
            Courses = await context.Courses.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Revisions = await context.CourseRevisions.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Modules = await context.CourseModules.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Reviews = await context.CourseReviews.AsNoTracking().OrderBy(x => x.Id).ToListAsync()
        });
    }

    private sealed class EnrollmentInsertGate : DbCommandInterceptor
    {
        private int armed;
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void Arm() => Interlocked.Exchange(ref armed, 1);

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("INSERT INTO \"Enrollments\"", StringComparison.Ordinal) &&
                command.CommandText.Contains("SELECT", StringComparison.Ordinal) && Interlocked.Exchange(ref armed, 0) == 1)
            {
                Entered.TrySetResult();
                await Release.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
            }
            return result;
        }
    }
}
