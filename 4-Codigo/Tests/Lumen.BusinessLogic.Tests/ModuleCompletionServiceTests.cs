using Lumen.BusinessLogic.Services;
using Lumen.DataAccess.Entities;
using Lumen.DataAccess.Repositories;
using Lumen.Shared.Enums;
using Moq;

namespace Lumen.BusinessLogic.Tests;

public sealed class ModuleCompletionServiceTests
{
    [Theory]
    [InlineData(3, 0, 33, ProgressStatus.EnProgreso)]
    [InlineData(201, 0, 1, ProgressStatus.EnProgreso)]
    [InlineData(200, 198, 99, ProgressStatus.EnProgreso)]
    [InlineData(200, 199, 100, ProgressStatus.Completado)]
    public async Task CompleteModuleAsync_UpdatesProgressUsingSharedCalculationAndPropagatesCancellation(
        int total, int completed, int expectedPercentage, ProgressStatus expectedStatus)
    {
        var enrollment = CreateEnrollment(total, completed);
        var moduleId = enrollment.Course.PublishedRevision!.Modules.ElementAt(completed).Id;
        using var cancellation = new CancellationTokenSource();
        var repository = new Mock<IEnrollmentRepository>();
        repository.Setup(x => x.GetByIdForUpdateAsync(enrollment.Id, cancellation.Token)).ReturnsAsync(enrollment);
        var service = new ProgressService(repository.Object, Mock.Of<ICourseRepository>(),
            new TestCurrentUser { UserId = enrollment.StudentId, Role = UserRole.Alumno });

        var result = await service.CompleteModuleAsync(enrollment.Id, moduleId, cancellation.Token);

        Assert.Equal(completed + 1, result.CompletedModules);
        Assert.Equal(total, result.TotalModules);
        Assert.Equal(expectedPercentage, result.Percentage);
        Assert.Equal(expectedStatus, result.Status);
        Assert.Contains(moduleId, result.CompletedModuleIds);
        var completion = Assert.Single(enrollment.ModuleCompletions, x => x.ModuleId == moduleId);
        Assert.Equal(enrollment.Id, completion.EnrollmentId);
        Assert.NotEqual(default, completion.CompletedAtUtc);
        repository.Verify(x => x.SaveAsync(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task CompleteModuleAsync_WhenAlreadyCompleted_KeepsOriginalCompletionWithoutSavingAgain()
    {
        var enrollment = CreateEnrollment(3, 1);
        var original = Assert.Single(enrollment.ModuleCompletions);
        var repository = new Mock<IEnrollmentRepository>();
        repository.Setup(x => x.GetByIdForUpdateAsync(enrollment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment);
        var service = new ProgressService(repository.Object, Mock.Of<ICourseRepository>(),
            new TestCurrentUser { UserId = enrollment.StudentId, Role = UserRole.Alumno });

        var result = await service.CompleteModuleAsync(enrollment.Id, original.ModuleId);

        Assert.Same(original, Assert.Single(enrollment.ModuleCompletions));
        Assert.Equal(original.ModuleId, Assert.Single(result.CompletedModuleIds));
        Assert.Equal(1, result.CompletedModules);
        Assert.Equal(33, result.Percentage);
        Assert.Equal(ProgressStatus.EnProgreso, result.Status);
        repository.Verify(x => x.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Enrollment CreateEnrollment(int total, int completed)
    {
        var course = new Course { Id = Guid.NewGuid() };
        var revision = new CourseRevision
        {
            Id = Guid.NewGuid(), CourseId = course.Id, Version = 1, Status = CourseRevisionStatus.Publicado
        };
        course.PublishedRevision = revision;
        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), CourseId = course.Id,
            Course = course, Status = EnrollmentStatus.Aprobada
        };
        for (var i = 0; i < total; i++)
        {
            var moduleId = Guid.NewGuid();
            revision.Modules.Add(new CourseModule { Id = moduleId, RevisionId = revision.Id });
            if (i < completed)
            {
                enrollment.ModuleCompletions.Add(new EnrollmentModuleCompletion
                {
                    EnrollmentId = enrollment.Id, ModuleId = moduleId, CompletedAtUtc = DateTime.UtcNow.AddDays(-1)
                });
            }
        }
        return enrollment;
    }
}
