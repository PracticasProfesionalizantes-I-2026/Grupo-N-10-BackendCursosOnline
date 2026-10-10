using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lumen.DataAccess;
using Lumen.DataAccess.Entities;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Lumen.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.API.IntegrationTests;

public sealed class ProgressIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Theory]
    [InlineData(201, 1, 1)]
    [InlineData(200, 199, 99)]
    public async Task Progress_WithPartialCompletions_ReturnsConsistentStatusAndPercentageOnBothEndpoints(
        int total, int completed, int expectedPercentage)
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        Guid enrollmentId;
        Guid courseId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
            var enrollment = await context.Enrollments
                .Include(x => x.ModuleCompletions)
                .Include(x => x.Course).ThenInclude(x => x.PublishedRevision).ThenInclude(x => x!.Modules)
                .SingleAsync(x => x.Status == EnrollmentStatus.Aprobada);
            var revision = enrollment.Course.PublishedRevision!;
            for (var i = revision.Modules.Count; i < total; i++)
            {
                revision.Modules.Add(new CourseModule
                {
                    Id = Guid.NewGuid(), RevisionId = revision.Id, Order = i + 1,
                    Name = $"Módulo {i + 1}", Description = "Descripción", Content = "Contenido", DurationMinutes = 1
                });
            }
            foreach (var module in revision.Modules.OrderBy(x => x.Order).Take(completed))
            {
                if (!enrollment.ModuleCompletions.Any(x => x.ModuleId == module.Id))
                {
                    enrollment.ModuleCompletions.Add(new EnrollmentModuleCompletion
                    {
                        EnrollmentId = enrollment.Id, ModuleId = module.Id, CompletedAtUtc = DateTime.UtcNow
                    });
                }
            }
            revision.TotalDurationMinutes = revision.Modules.Sum(x => x.DurationMinutes);
            await context.SaveChangesAsync();
            enrollmentId = enrollment.Id;
            courseId = enrollment.CourseId;
        }
        await AuthenticateAsync(client, "alumno@lumen.local");

        var response = await client.GetAsync($"/api/enrollments/{enrollmentId}/progress");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var progress = await response.Content.ReadFromJsonAsync<ProgressResponseDTO>(JsonOptions);
        Assert.NotNull(progress);
        Assert.Equal(ProgressStatus.EnProgreso, progress.Status);
        Assert.Equal(expectedPercentage, progress.Percentage);
        Assert.Equal(total, progress.TotalModules);
        Assert.Equal(completed, progress.CompletedModules);
        await AuthenticateAsync(client, "profesor@lumen.local");
        var students = await client.GetFromJsonAsync<List<StudentProgressResponseDTO>>(
            $"/api/courses/{courseId}/students/progress", JsonOptions);
        var courseProgress = Assert.Single(students!).Progress;
        Assert.Equal(progress.Status, courseProgress.Status);
        Assert.Equal(progress.Percentage, courseProgress.Percentage);
        Assert.Equal(progress.CompletedModules, courseProgress.CompletedModules);
        Assert.Equal(progress.TotalModules, courseProgress.TotalModules);
    }

    [Theory]
    [InlineData("profesor@lumen.local", false, HttpStatusCode.OK)]
    [InlineData("admin@lumen.local", false, HttpStatusCode.OK)]
    [InlineData("profesor@lumen.local", true, HttpStatusCode.Forbidden)]
    [InlineData("alumno@lumen.local", true, HttpStatusCode.Forbidden)]
    public async Task Progress_EnforcesActorOwnership(string email, bool belongsToAnotherUser, HttpStatusCode expected)
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        Guid enrollmentId;
        Guid courseId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
            var enrollment = await context.Enrollments.Include(x => x.Course)
                .SingleAsync(x => x.Status == EnrollmentStatus.Aprobada);
            if (belongsToAnotherUser)
            {
                var actor = await context.Users.SingleAsync(x => x.Email == email);
                var otherUser = new User
                {
                    Id = Guid.NewGuid(), Email = "otro@lumen.local", NormalizedEmail = "OTRO@LUMEN.LOCAL",
                    Role = actor.Role, PasswordHash = actor.PasswordHash, FirstName = "Otro", LastName = "Usuario",
                    Dni = "99999999", Phone = actor.Phone, Address = actor.Address, PostalCode = actor.PostalCode
                };
                context.Users.Add(otherUser);
                if (actor.Role == UserRole.Alumno)
                    enrollment.StudentId = otherUser.Id;
                else
                    enrollment.Course.OwnerProfessorId = otherUser.Id;
                await context.SaveChangesAsync();
            }
            enrollmentId = enrollment.Id;
            courseId = enrollment.CourseId;
        }
        await AuthenticateAsync(client, email);

        var response = await client.GetAsync($"/api/enrollments/{enrollmentId}/progress");

        Assert.Equal(expected, response.StatusCode);
        if (expected == HttpStatusCode.Forbidden)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponseDTO>(JsonOptions);
            Assert.Equal(email.StartsWith("alumno") ? nameof(EnrollmentOwnershipException) : nameof(CourseOwnershipException),
                error?.Code);
        }
        var courseResponse = await client.GetAsync($"/api/courses/{courseId}/students/progress");
        Assert.Equal(expected, courseResponse.StatusCode);
    }

    private static async Task AuthenticateAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDTO(email, DbInitializer.DemoPassword), JsonOptions);
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponseDTO>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
    }
}
