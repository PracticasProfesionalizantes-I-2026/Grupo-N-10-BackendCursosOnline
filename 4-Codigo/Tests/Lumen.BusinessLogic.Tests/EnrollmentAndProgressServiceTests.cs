using Lumen.BusinessLogic.Services;
using Lumen.DataAccess.Entities;
using Lumen.DataAccess.Repositories;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Lumen.Shared.Exceptions;
using Moq;

namespace Lumen.BusinessLogic.Tests;

public sealed class EnrollmentAndProgressServiceTests
{
    [Fact]
    public async Task CreateEnrollment_RejectsDuplicatePendingOrApproved()
    {
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var courses = new Mock<ICourseRepository>();
        courses.Setup(x => x.GetPublishedByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Course { Id = courseId });
        var enrollments = new Mock<IEnrollmentRepository>();
        enrollments.Setup(x => x.HasPendingOrApprovedAsync(studentId, courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = new EnrollmentService(
            enrollments.Object, courses.Object,
            new TestCurrentUser { UserId = studentId, Role = UserRole.Alumno });

        await Assert.ThrowsAsync<DuplicateEnrollmentException>(() =>
            service.CreateAsync(new EnrollmentCreateDTO(courseId)));
    }

    [Fact]
    public async Task Progress_UsesCompletedModulesFromCurrentPublishedRevision()
    {
        var studentId = Guid.NewGuid();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var revision = new CourseRevision { Version = 3, Status = CourseRevisionStatus.Publicado };
        revision.Modules.Add(new CourseModule { Id = first, RevisionId = revision.Id });
        revision.Modules.Add(new CourseModule { Id = second, RevisionId = revision.Id });
        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(), StudentId = studentId, Status = EnrollmentStatus.Aprobada,
            Course = new Course { PublishedRevision = revision }
        };
        enrollment.ModuleCompletions.Add(new EnrollmentModuleCompletion { EnrollmentId = enrollment.Id, ModuleId = first });
        var repository = new Mock<IEnrollmentRepository>();
        repository.Setup(x => x.GetByIdAsync(enrollment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(enrollment);
        var service = new ProgressService(
            repository.Object, Mock.Of<ICourseRepository>(),
            new TestCurrentUser { UserId = studentId, Role = UserRole.Alumno });

        var result = await service.GetAsync(enrollment.Id);

        Assert.Equal(50, result.Percentage);
        Assert.Equal(ProgressStatus.EnProgreso, result.Status);
        Assert.Equal(3, result.PublishedCourseVersion);
    }

    [Fact]
    public async Task Progress_IsBlockedWhenEnrollmentIsCanceled()
    {
        var studentId = Guid.NewGuid();
        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(), StudentId = studentId, Status = EnrollmentStatus.Cancelada,
            Course = new Course { PublishedRevision = new CourseRevision() }
        };
        var repository = new Mock<IEnrollmentRepository>();
        repository.Setup(x => x.GetByIdAsync(enrollment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(enrollment);
        var service = new ProgressService(
            repository.Object, Mock.Of<ICourseRepository>(),
            new TestCurrentUser { UserId = studentId, Role = UserRole.Alumno });

        await Assert.ThrowsAsync<EnrollmentStateConflictException>(() => service.GetAsync(enrollment.Id));
    }
}

