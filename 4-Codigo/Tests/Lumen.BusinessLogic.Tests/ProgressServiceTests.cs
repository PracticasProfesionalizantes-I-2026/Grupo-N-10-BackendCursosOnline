using Lumen.BusinessLogic.Services;
using Lumen.DataAccess.Entities;
using Lumen.DataAccess.Repositories;
using Lumen.Shared.Enums;
using Moq;

namespace Lumen.BusinessLogic.Tests;

public sealed class ProgressServiceTests
{
    [Theory]
    [InlineData(3, 0, 0, ProgressStatus.NoIniciado)]
    [InlineData(201, 1, 1, ProgressStatus.EnProgreso)]
    [InlineData(200, 199, 99, ProgressStatus.EnProgreso)]
    [InlineData(200, 200, 100, ProgressStatus.Completado)]
    [InlineData(8, 1, 12, ProgressStatus.EnProgreso)]
    [InlineData(3, 2, 67, ProgressStatus.EnProgreso)]
    [InlineData(0, 0, 0, ProgressStatus.NoIniciado)]
    public async Task GetAsync_DeterminesStatusFromCompletionsAndKeepsPartialPercentageBetweenOneAndNinetyNine(
        int total, int completed, int expectedPercentage, ProgressStatus expectedStatus)
    {
        var enrollment = CreateEnrollment(total, completed);
        var repository = new Mock<IEnrollmentRepository>();
        using var cancellation = new CancellationTokenSource();
        repository.Setup(x => x.GetByIdAsync(enrollment.Id, cancellation.Token)).ReturnsAsync(enrollment);
        var service = new ProgressService(repository.Object, Mock.Of<ICourseRepository>(),
            new TestCurrentUser { UserId = enrollment.StudentId, Role = UserRole.Alumno });

        var result = await service.GetAsync(enrollment.Id, cancellation.Token);

        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(expectedPercentage, result.Percentage);
        Assert.Equal(total, result.TotalModules);
        Assert.Equal(completed, result.CompletedModules);
        Assert.Equal(enrollment.ModuleCompletions.Select(x => x.ModuleId), result.CompletedModuleIds);
        Assert.Equal(enrollment.Id, result.EnrollmentId);
        Assert.Equal(enrollment.StudentId, result.StudentId);
        Assert.Equal(enrollment.CourseId, result.CourseId);
        Assert.Equal(3, result.PublishedCourseVersion);
        repository.Verify(x => x.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAsync_CountsDistinctCompletionsOnlyFromPublishedRevision()
    {
        var enrollment = CreateEnrollment(2, 1);
        var workingModuleId = Guid.NewGuid();
        enrollment.Course.WorkingRevision = new CourseRevision
        {
            Version = 4, Status = CourseRevisionStatus.Borrador,
            Modules = [new CourseModule { Id = workingModuleId }]
        };
        enrollment.ModuleCompletions.Add(new EnrollmentModuleCompletion
        {
            EnrollmentId = enrollment.Id, ModuleId = enrollment.ModuleCompletions.First().ModuleId
        });
        enrollment.ModuleCompletions.Add(new EnrollmentModuleCompletion
        {
            EnrollmentId = enrollment.Id, ModuleId = workingModuleId
        });
        var repository = new Mock<IEnrollmentRepository>();
        repository.Setup(x => x.GetByIdAsync(enrollment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(enrollment);
        var service = new ProgressService(repository.Object, Mock.Of<ICourseRepository>(),
            new TestCurrentUser { UserId = enrollment.StudentId, Role = UserRole.Alumno });

        var result = await service.GetAsync(enrollment.Id);

        Assert.Equal(1, result.CompletedModules);
        Assert.Equal(2, result.TotalModules);
        Assert.Equal(50, result.Percentage);
        Assert.Equal(ProgressStatus.EnProgreso, result.Status);
        Assert.Equal(3, result.PublishedCourseVersion);
        Assert.Equal(enrollment.ModuleCompletions.First().ModuleId, Assert.Single(result.CompletedModuleIds));
    }

    private static Enrollment CreateEnrollment(int total, int completed)
    {
        var course = new Course { Id = Guid.NewGuid() };
        var revision = new CourseRevision
        {
            Id = Guid.NewGuid(), CourseId = course.Id, Version = 3, Status = CourseRevisionStatus.Publicado
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
                    EnrollmentId = enrollment.Id, ModuleId = moduleId
                });
            }
        }
        return enrollment;
    }
}
