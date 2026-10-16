using System.Net;
using System.Net.Http.Json;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Xunit.Abstractions;
using static Lumen.API.IntegrationTests.ConcurrencyTestSupport;

namespace Lumen.API.IntegrationTests;

public sealed class EnrollmentConcurrencyRegressionTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CU13_OverlappingCancellations_PreserveFirstActorAndRejectStaleCancellation(bool lateIsAdministrator)
    {
        var gate = new ReadGate();
        using var factory = CreateFactory(gate);
        using var student = factory.CreateClient();
        var studentUser = await AuthenticateAsync(student, "alumno@lumen.local");
        using var admin = factory.CreateClient();
        var adminUser = await AuthenticateAsync(admin, "admin@lumen.local");
        var scenario = await PrepareAsync(factory);
        var lateClient = lateIsAdministrator ? admin : student;
        var winnerClient = lateIsAdministrator ? student : admin;
        var winnerId = lateIsAdministrator ? studentUser.Id : adminUser.Id;
        gate.ArmEnrollment(scenario.EnrollmentId);
        var lateTask = CaptureAsync(() => lateClient.PostAsync($"/api/enrollments/{scenario.EnrollmentId}/cancel", null));
        try
        {
            await gate.Entered.Task.WaitAsync(Deadline);
            Assert.Equal(EnrollmentStatus.Aprobada, gate.ReadStatus);
            using var winner = await CaptureAsync(() => winnerClient.PostAsync($"/api/enrollments/{scenario.EnrollmentId}/cancel", null));
            AssertHttp(winner, HttpStatusCode.OK);
            var firstSaved = await ReadAsync(factory, scenario);
            Assert.Equal(winnerId, firstSaved.CanceledByUserId);
            Assert.NotNull(firstSaved.CanceledAtUtc);
            gate.Release.TrySetResult();
            using var late = await lateTask.WaitAsync(Deadline);
            var final = await ReadAsync(factory, scenario);
            Report(output, "Winning cancellation", winner);
            Report(output, "Stale cancellation", late);
            output.WriteLine($"Original cancellation actor preserved: {final.CanceledByUserId == winnerId}");
            Assert.Multiple(
                () => AssertHttp(late, HttpStatusCode.Conflict),
                () => Assert.Equal(2, gate.Contexts.Count),
                () => Assert.Equal(EnrollmentStatus.Cancelada, final.Status),
                () => Assert.Equal(firstSaved.CanceledByUserId, final.CanceledByUserId),
                () => Assert.Equal(firstSaved.CanceledAtUtc, final.CanceledAtUtc),
                () => Assert.Empty(final.Completions));
        }
        finally { gate.Release.TrySetResult(); await ObserveAsync(lateTask); }
    }

    [Fact]
    public async Task CU16_CompletionReadBeforeCancellation_DoesNotPersistAdvanceAfterCancellation()
    {
        var gate = new ReadGate();
        using var factory = CreateFactory(gate);
        using var student = factory.CreateClient();
        await AuthenticateAsync(student, "alumno@lumen.local");
        using var admin = factory.CreateClient();
        var adminUser = await AuthenticateAsync(admin, "admin@lumen.local");
        var scenario = await PrepareAsync(factory);
        gate.ArmEnrollment(scenario.EnrollmentId);
        var completionTask = CaptureAsync(() => student.PutAsync(CompletionUrl(scenario, scenario.ModuleIds[0]), null));
        try
        {
            await gate.Entered.Task.WaitAsync(Deadline);
            Assert.Equal(EnrollmentStatus.Aprobada, gate.ReadStatus);
            Assert.Empty(gate.ReadCompletedModules);
            using var cancel = await admin.PostAsync($"/api/enrollments/{scenario.EnrollmentId}/cancel", null);
            Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
            var canceled = await ReadAsync(factory, scenario);
            Assert.Equal(EnrollmentStatus.Cancelada, canceled.Status);
            Assert.Empty(canceled.Completions);
            gate.Release.TrySetResult();
            using var completion = await completionTask.WaitAsync(Deadline);
            var final = await ReadAsync(factory, scenario);
            using var progress = await student.GetAsync($"/api/enrollments/{scenario.EnrollmentId}/progress");
            Report(output, "Completion after committed cancellation", completion);
            output.WriteLine($"Persisted completions after cancellation: {final.Completions.Length}");
            Assert.Multiple(
                () => AssertHttp(completion, HttpStatusCode.Conflict),
                () => Assert.Equal(2, gate.Contexts.Count),
                () => Assert.Equal(EnrollmentStatus.Cancelada, final.Status),
                () => Assert.Equal(adminUser.Id, final.CanceledByUserId),
                () => Assert.Equal(canceled.CanceledAtUtc, final.CanceledAtUtc),
                () => Assert.Empty(final.Completions),
                () => Assert.Equal(HttpStatusCode.Conflict, progress.StatusCode));
        }
        finally { gate.Release.TrySetResult(); await ObserveAsync(completionTask); }
    }

    [Fact]
    public async Task CU16_CompletionReadBeforeNewPublication_RejectsModuleRemovedFromCurrentPublication()
    {
        var gate = new ReadGate();
        using var factory = CreateFactory(gate);
        using var student = factory.CreateClient();
        await AuthenticateAsync(student, "alumno@lumen.local");
        using var teacher = factory.CreateClient();
        await AuthenticateAsync(teacher, "profesor@lumen.local");
        using var admin = factory.CreateClient();
        await AuthenticateAsync(admin, "admin@lumen.local");
        var scenario = await PrepareAsync(factory);
        var course = await teacher.GetFromJsonAsync<CourseResponseDTO>($"/api/courses/{scenario.CourseId}", Json);
        Assert.NotNull(course);
        var removedModule = scenario.ModuleIds[0];
        var update = new CourseUpdateDTO(course.Title, course.Description, course.Category, course.Level,
            course.Modality, course.MaxCapacity, course.LearningObjectives, course.SuggestedPrerequisites,
            course.Modules.Where(x => x.Id != removedModule).Select(x => new CourseModuleUpdateDTO(
                x.Id, x.Name, x.Description, x.DurationMinutes, x.Content!, x.Resources)).ToList());
        using var edited = await teacher.PutAsJsonAsync($"/api/courses/{scenario.CourseId}", update, Json);
        Assert.Equal(HttpStatusCode.OK, edited.StatusCode);
        using var submitted = await teacher.PostAsync($"/api/courses/{scenario.CourseId}/submit-review", null);
        Assert.Equal(HttpStatusCode.OK, submitted.StatusCode);
        var review = await submitted.Content.ReadFromJsonAsync<CourseReviewResponseDTO>(Json);
        Assert.NotNull(review);
        gate.ArmEnrollment(scenario.EnrollmentId);
        var completionTask = CaptureAsync(() => student.PutAsync(CompletionUrl(scenario, removedModule), null));
        try
        {
            await gate.Entered.Task.WaitAsync(Deadline);
            Assert.Equal(scenario.PublicationId, gate.ReadPublicationId);
            using var published = await admin.PostAsJsonAsync($"/api/course-reviews/{review.Id}/decision",
                new CourseReviewDecisionDTO(CourseReviewDecision.Aprobada, null), Json);
            Assert.Equal(HttpStatusCode.OK, published.StatusCode);
            var beforeRelease = await ReadAsync(factory, scenario);
            Assert.Equal(review.RevisionId, beforeRelease.PublicationId);
            var content = await student.GetFromJsonAsync<PublishedCourseResponseDTO>(
                $"/api/enrollments/{scenario.EnrollmentId}/course-content", Json);
            Assert.NotNull(content);
            Assert.DoesNotContain(content.Modules, x => x.Id == removedModule);
            gate.Release.TrySetResult();
            using var completion = await completionTask.WaitAsync(Deadline);
            var final = await ReadAsync(factory, scenario);
            var progress = await student.GetFromJsonAsync<ProgressResponseDTO>(
                $"/api/enrollments/{scenario.EnrollmentId}/progress", Json);
            Report(output, "Completion after committed new publication", completion);
            output.WriteLine($"Persisted removed-module completions: {final.Completions.Count(x => x.ModuleId == removedModule)}");
            Assert.Multiple(
                () => AssertHttp(completion, HttpStatusCode.Conflict),
                () => Assert.Equal(EnrollmentStatus.Aprobada, final.Status),
                () => Assert.Equal(review.RevisionId, final.PublicationId),
                () => Assert.Empty(final.Completions),
                () => { Assert.NotNull(progress); Assert.Equal(content.Version, progress.PublishedCourseVersion);
                    Assert.Equal(0, progress.CompletedModules); Assert.DoesNotContain(removedModule, progress.CompletedModuleIds); });
        }
        finally { gate.Release.TrySetResult(); await ObserveAsync(completionTask); }
    }

    [Fact]
    public Task CU16_OverlappingSameModuleCompletions_AreIdempotentAndPreserveTimestamp() => RunCompletionsAsync(sameModule: true);

    [Fact]
    public Task CU16_OverlappingDifferentModuleCompletions_ReturnProgressIncludingBothCommittedModules() => RunCompletionsAsync(sameModule: false);

    private async Task RunCompletionsAsync(bool sameModule)
    {
        var gate = new ReadGate();
        using var factory = CreateFactory(gate);
        using var lateStudent = factory.CreateClient();
        await AuthenticateAsync(lateStudent, "alumno@lumen.local");
        using var winnerStudent = factory.CreateClient();
        await AuthenticateAsync(winnerStudent, "alumno@lumen.local");
        var scenario = await PrepareAsync(factory);
        var lateModule = scenario.ModuleIds[sameModule ? 0 : 1];
        gate.ArmEnrollment(scenario.EnrollmentId);
        var lateTask = CaptureAsync(() => lateStudent.PutAsync(CompletionUrl(scenario, lateModule), null));
        try
        {
            await gate.Entered.Task.WaitAsync(Deadline);
            Assert.Empty(gate.ReadCompletedModules);
            using var winner = await CaptureAsync(() => winnerStudent.PutAsync(CompletionUrl(scenario, scenario.ModuleIds[0]), null));
            AssertHttp(winner, HttpStatusCode.OK);
            var firstSaved = Assert.Single((await ReadAsync(factory, scenario)).Completions);
            gate.Release.TrySetResult();
            using var late = await lateTask.WaitAsync(Deadline);
            var final = await ReadAsync(factory, scenario);
            var queried = await lateStudent.GetFromJsonAsync<ProgressResponseDTO>(
                $"/api/enrollments/{scenario.EnrollmentId}/progress", Json);
            var returned = late.Response?.IsSuccessStatusCode == true
                ? await late.Response.Content.ReadFromJsonAsync<ProgressResponseDTO>(Json) : null;
            var expectedModules = sameModule ? new[] { scenario.ModuleIds[0] } : new[] { scenario.ModuleIds[0], lateModule };
            var expectedPercentage = sameModule ? 33 : 67;
            Report(output, "Winning completion", winner);
            Report(output, "Stale completion", late);
            output.WriteLine($"Persisted modules: {final.Completions.Length}; returned={returned?.CompletedModules}; queried={queried?.CompletedModules}");
            Assert.Multiple(
                () => AssertHttp(late, HttpStatusCode.OK),
                () => Assert.Equal(2, gate.Contexts.Count),
                () => Assert.Equal(expectedModules.Order(), final.Completions.Select(x => x.ModuleId).Order()),
                () => Assert.Equal(firstSaved.CompletedAtUtc, Assert.Single(final.Completions, x => x.ModuleId == firstSaved.ModuleId).CompletedAtUtc),
                () => AssertProgress(queried, expectedModules, expectedPercentage, scenario),
                () => AssertProgress(returned, expectedModules, expectedPercentage, scenario));
        }
        finally { gate.Release.TrySetResult(); await ObserveAsync(lateTask); }
    }

    private static void AssertProgress(ProgressResponseDTO? progress, Guid[] modules, int percentage, Scenario scenario)
    {
        Assert.NotNull(progress);
        Assert.Equal(scenario.EnrollmentId, progress.EnrollmentId);
        Assert.Equal(scenario.StudentId, progress.StudentId);
        Assert.Equal(scenario.CourseId, progress.CourseId);
        Assert.Equal(scenario.Version, progress.PublishedCourseVersion);
        Assert.Equal(3, progress.TotalModules);
        Assert.Equal(modules.Length, progress.CompletedModules);
        Assert.Equal(modules.Order(), progress.CompletedModuleIds.Order());
        Assert.Equal(percentage, progress.Percentage);
        Assert.Equal(ProgressStatus.EnProgreso, progress.Status);
    }
}
