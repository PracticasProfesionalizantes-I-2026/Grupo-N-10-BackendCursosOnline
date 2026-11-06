using System.Net;
using System.Net.Http.Json;
using Lumen.DataAccess;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;

namespace Lumen.API.IntegrationTests;

public sealed class CourseClosureEnrollmentIntegrationTests
{
    [Theory]
    [InlineData("pause")]
    [InlineData("finish")]
    public async Task Closure_KeepsExistingApprovedAccessAndPendingDecision(string transition)
    {
        using var factory = new LumenApiFactory();
        using var admin = await CourseTestClient.LoginAsync(factory, DemoAccounts.AdministratorEmail);
        using var student = await CourseTestClient.LoginAsync(factory, DemoAccounts.StudentEmail);
        var enrollments = (await student.GetFromJsonAsync<EnrollmentResponseDTO[]>("/api/enrollments/mine", CourseTestClient.Json))!;
        var approved = Assert.Single(enrollments, x => x.Status == EnrollmentStatus.Aprobada);
        var pending = Assert.Single(enrollments, x => x.Status == EnrollmentStatus.Pendiente);
        foreach (var enrollment in new[] { approved, pending })
        {
            using var closed = await admin.PostAsync($"/api/courses/{enrollment.CourseId}/{transition}", null);
            Assert.Equal(HttpStatusCode.OK, closed.StatusCode);
        }
        using var existingAccess = await student.GetAsync($"/api/enrollments/{approved.Id}/course-content");
        Assert.Equal(HttpStatusCode.OK, existingAccess.StatusCode);
        var pendingAfter = (await admin.GetFromJsonAsync<EnrollmentResponseDTO>(
            $"/api/enrollments/{pending.Id}", CourseTestClient.Json))!;
        Assert.Equal(EnrollmentStatus.Pendiente, pendingAfter.Status);
        using var resolved = await admin.PostAsJsonAsync($"/api/enrollments/{pending.Id}/decision",
            new EnrollmentDecisionDTO(EnrollmentStatus.Aprobada), CourseTestClient.Json);
        Assert.Equal(HttpStatusCode.OK, resolved.StatusCode);
        using var granted = await student.GetAsync($"/api/enrollments/{pending.Id}/course-content");
        Assert.Equal(HttpStatusCode.OK, granted.StatusCode);
    }
}
