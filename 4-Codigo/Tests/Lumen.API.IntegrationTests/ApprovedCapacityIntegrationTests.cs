using System.Net;
using System.Net.Http.Json;
using Lumen.DataAccess;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.API.IntegrationTests;

public sealed class ApprovedCapacityIntegrationTests
{
    [Fact]
    public async Task ConcurrentApprovals_OnlyOneTakesTheLastPlace()
    {
        using var factory = new LumenApiFactory();
        using var student = await CourseTestClient.LoginAsync(factory, DemoAccounts.StudentEmail);
        using var firstAdmin = await CourseTestClient.LoginAsync(factory, DemoAccounts.AdministratorEmail);
        using var secondAdmin = await CourseTestClient.LoginAsync(factory, DemoAccounts.AdministratorEmail);
        var existing = (await student.GetFromJsonAsync<EnrollmentResponseDTO[]>("/api/enrollments/mine", CourseTestClient.Json))!;
        var pending = Assert.Single(existing, x => x.Status == EnrollmentStatus.Pendiente);
        var extra = await CreatePendingAsync(factory, pending.CourseId);
        await SetPublishedCapacityAsync(factory, pending.CourseId, 1);

        var first = firstAdmin.PostAsJsonAsync($"/api/enrollments/{pending.Id}/decision",
            new EnrollmentDecisionDTO(EnrollmentStatus.Aprobada), CourseTestClient.Json);
        var second = secondAdmin.PostAsJsonAsync($"/api/enrollments/{extra.Id}/decision",
            new EnrollmentDecisionDTO(EnrollmentStatus.Aprobada), CourseTestClient.Json);
        using var firstResponse = await first;
        using var secondResponse = await second;
        Assert.Contains(HttpStatusCode.OK, new[] { firstResponse.StatusCode, secondResponse.StatusCode });
        Assert.Contains(HttpStatusCode.Conflict, new[] { firstResponse.StatusCode, secondResponse.StatusCode });
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        Assert.Equal(1, await db.Enrollments.CountAsync(x => x.CourseId == pending.CourseId &&
            x.Status == EnrollmentStatus.Aprobada));
        Assert.Equal(1, await db.Enrollments.CountAsync(x => x.CourseId == pending.CourseId &&
            x.Status == EnrollmentStatus.Pendiente));
    }

    [Fact]
    public async Task Finish_RejectsEveryPendingWithReasonAndPreservesApprovedAccess()
    {
        using var factory = new LumenApiFactory();
        using var student = await CourseTestClient.LoginAsync(factory, DemoAccounts.StudentEmail);
        using var admin = await CourseTestClient.LoginAsync(factory, DemoAccounts.AdministratorEmail);
        var existing = (await student.GetFromJsonAsync<EnrollmentResponseDTO[]>("/api/enrollments/mine", CourseTestClient.Json))!;
        var pending = Assert.Single(existing, x => x.Status == EnrollmentStatus.Pendiente);
        var approved = Assert.Single(existing, x => x.Status == EnrollmentStatus.Aprobada);
        var extra = await CreatePendingAsync(factory, pending.CourseId);

        using var finished = await admin.PostAsync($"/api/courses/{pending.CourseId}/finish", null);
        Assert.Equal(HttpStatusCode.OK, finished.StatusCode);
        foreach (var id in new[] { pending.Id, extra.Id })
        {
            var result = await admin.GetFromJsonAsync<EnrollmentResponseDTO>($"/api/enrollments/{id}", CourseTestClient.Json);
            Assert.Equal(EnrollmentStatus.Rechazada, result!.Status);
            Assert.Equal("Curso finalizado.", result.ResolutionReason);
        }
        using var repeated = await admin.PostAsync($"/api/courses/{pending.CourseId}/finish", null);
        Assert.Equal(HttpStatusCode.Conflict, repeated.StatusCode);
        using var access = await student.GetAsync($"/api/enrollments/{approved.Id}/course-content");
        Assert.Equal(HttpStatusCode.OK, access.StatusCode);
    }

    [Fact]
    public async Task EditingCapacity_BelowApprovedOccupancyReturnsConflictWithoutChangingCourse()
    {
        using var factory = new LumenApiFactory();
        using var student = await CourseTestClient.LoginAsync(factory, DemoAccounts.StudentEmail);
        using var teacher = await CourseTestClient.LoginAsync(factory);
        using var admin = await CourseTestClient.LoginAsync(factory, DemoAccounts.AdministratorEmail);
        var existing = (await student.GetFromJsonAsync<EnrollmentResponseDTO[]>("/api/enrollments/mine", CourseTestClient.Json))!;
        var pending = Assert.Single(existing, x => x.Status == EnrollmentStatus.Pendiente);
        var extra = await CreatePendingAsync(factory, pending.CourseId);
        await SetPublishedCapacityAsync(factory, pending.CourseId, 2);
        foreach (var id in new[] { pending.Id, extra.Id })
        {
            using var approved = await admin.PostAsJsonAsync($"/api/enrollments/{id}/decision",
                new EnrollmentDecisionDTO(EnrollmentStatus.Aprobada), CourseTestClient.Json);
            Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        }
        var course = await CourseTestClient.GetAsync(teacher, pending.CourseId);
        using var reduced = await CourseTestClient.SendEditAsync(teacher, pending.CourseId,
            CourseTestClient.Update(course) with { MaxCapacity = 1 },
            await CourseTestClient.ReadTagAsync(teacher, pending.CourseId));
        Assert.Equal(HttpStatusCode.Conflict, reduced.StatusCode);
        Assert.Equal(2, (await CourseTestClient.GetAsync(teacher, pending.CourseId)).MaxCapacity);
    }

    [Fact]
    public async Task Finish_RollsBackCourseWhenPendingRejectionFails()
    {
        using var factory = new LumenApiFactory();
        using var student = await CourseTestClient.LoginAsync(factory, DemoAccounts.StudentEmail);
        using var admin = await CourseTestClient.LoginAsync(factory, DemoAccounts.AdministratorEmail);
        var mine = (await student.GetFromJsonAsync<EnrollmentResponseDTO[]>("/api/enrollments/mine", CourseTestClient.Json))!;
        var pending = Assert.Single(mine, x => x.Status == EnrollmentStatus.Pendiente);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TRIGGER TR_Test_FailAutomaticRejection
                BEFORE UPDATE OF Status ON Enrollments
                WHEN OLD.Status = 'Pendiente' AND NEW.Status = 'Rechazada'
                BEGIN SELECT RAISE(ABORT, 'test failure'); END;
                """);
        }
        using var failed = await admin.PostAsync($"/api/courses/{pending.CourseId}/finish", null);
        Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode);
        await using var verification = factory.Services.CreateAsyncScope();
        var context = verification.ServiceProvider.GetRequiredService<LumenDbContext>();
        Assert.Equal(CourseOperationalStatus.Activo,
            (await context.Courses.AsNoTracking().SingleAsync(x => x.Id == pending.CourseId)).OperationalStatus);
        Assert.Equal(EnrollmentStatus.Pendiente,
            (await context.Enrollments.AsNoTracking().SingleAsync(x => x.Id == pending.Id)).Status);
    }

    private static async Task<EnrollmentResponseDTO> CreatePendingAsync(LumenApiFactory factory, Guid courseId)
    {
        var email = $"capacity-{Guid.NewGuid():N}@example.test";
        using var guest = factory.CreateClient();
        using var registration = await guest.PostAsJsonAsync("/api/auth/register", new UserCreateDTO(
            email, DbInitializer.DemoPassword, UserRole.Alumno, "Alumno", "Cupo",
            "01234567", "11-1234-5678", "Calle 1", "C1000ABC"), CourseTestClient.Json);
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        using var student = await CourseTestClient.LoginAsync(factory, email);
        using var created = await student.PostAsJsonAsync("/api/enrollments", new EnrollmentCreateDTO(courseId), CourseTestClient.Json);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return (await created.Content.ReadFromJsonAsync<EnrollmentResponseDTO>(CourseTestClient.Json))!;
    }

    private static async Task SetPublishedCapacityAsync(LumenApiFactory factory, Guid courseId, int capacity)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        var course = await db.Courses.AsNoTracking().SingleAsync(x => x.Id == courseId);
        await db.CourseRevisions.Where(x => x.Id == course.PublishedRevisionId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.MaxCapacity, capacity));
    }
}
