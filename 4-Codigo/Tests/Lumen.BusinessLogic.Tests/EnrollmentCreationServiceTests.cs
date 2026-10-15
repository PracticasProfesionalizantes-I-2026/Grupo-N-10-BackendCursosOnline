using Lumen.BusinessLogic.Services;
using Lumen.DataAccess.Entities;
using Lumen.DataAccess.Repositories;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Lumen.Shared.Exceptions;
using Moq;

namespace Lumen.BusinessLogic.Tests;

public sealed class EnrollmentCreationServiceTests
{
    [Fact]
    public async Task Create_UsesAuthenticatedStudentAndPersistsPendingWithoutCourseGraph()
    {
        var student = new TestCurrentUser { Role = UserRole.Alumno };
        var course = new Course
        {
            Id = Guid.NewGuid(), PublishedRevisionId = Guid.NewGuid(),
            PublishedRevision = new CourseRevision { Title = "Versión pública" },
            WorkingRevision = new CourseRevision { Title = "Cambios en revisión", Status = CourseRevisionStatus.EnRevision }
        };
        var courses = new Mock<ICourseRepository>();
        courses.Setup(x => x.GetPublishedByIdAsync(course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(course);
        var enrollments = new Mock<IEnrollmentRepository>();
        var createdId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        var before = DateTime.UtcNow;
        enrollments.Setup(x => x.TryCreateForPublishedCourseAsync(
                It.IsAny<Enrollment>(), CourseOperationalStatus.Activo, cancellation.Token))
            .Callback<Enrollment, CourseOperationalStatus, CancellationToken>((entity, _, _) =>
            {
                Assert.Equal(student.UserId, entity.StudentId);
                Assert.Equal(course.Id, entity.CourseId);
                Assert.Equal(EnrollmentStatus.Pendiente, entity.Status);
                Assert.Null(entity.Course);
                Assert.Null(entity.ResolvedAtUtc);
                Assert.Null(entity.ResolvedByUserId);
                Assert.Null(entity.CanceledAtUtc);
                Assert.Null(entity.CanceledByUserId);
                Assert.Empty(entity.ModuleCompletions);
                entity.Id = createdId;
            }).ReturnsAsync(true);
        var service = new EnrollmentService(enrollments.Object, courses.Object, student);

        var result = await service.CreateAsync(new EnrollmentCreateDTO(course.Id), cancellation.Token);

        Assert.Equal(createdId, result.Id);
        Assert.Equal(student.UserId, result.StudentId);
        Assert.Equal(EnrollmentStatus.Pendiente, result.Status);
        Assert.Equal("Versión pública", result.CourseTitle);
        Assert.InRange(result.RequestedAtUtc, before, DateTime.UtcNow);
        enrollments.Verify(x => x.HasPendingOrApprovedAsync(student.UserId, course.Id, cancellation.Token), Times.Once);
        enrollments.Verify(x => x.TryCreateForPublishedCourseAsync(
            It.IsAny<Enrollment>(), CourseOperationalStatus.Activo, cancellation.Token), Times.Once);
        enrollments.Verify(x => x.CreateAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(UserRole.Profesor, true)]
    [InlineData(UserRole.Administrador, true)]
    [InlineData(UserRole.Alumno, false)]
    public async Task Create_RejectsUnauthorizedActorBeforeAccessingRepositories(UserRole role, bool authenticated)
    {
        var courses = new Mock<ICourseRepository>(MockBehavior.Strict);
        var enrollments = new Mock<IEnrollmentRepository>(MockBehavior.Strict);
        var service = new EnrollmentService(enrollments.Object, courses.Object,
            new TestCurrentUser { Role = role, IsAuthenticated = authenticated });

        await Assert.ThrowsAsync<ForbiddenOperationException>(() => service.CreateAsync(new EnrollmentCreateDTO(Guid.NewGuid())));
        courses.VerifyNoOtherCalls();
        enrollments.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_RejectsEmptyStudentIdentityBeforeAccessingRepositories()
    {
        var courses = new Mock<ICourseRepository>(MockBehavior.Strict);
        var enrollments = new Mock<IEnrollmentRepository>(MockBehavior.Strict);
        var service = new EnrollmentService(enrollments.Object, courses.Object,
            new TestCurrentUser { Role = UserRole.Alumno, UserId = Guid.Empty });

        await Assert.ThrowsAsync<ForbiddenOperationException>(() => service.CreateAsync(new EnrollmentCreateDTO(Guid.NewGuid())));
        courses.VerifyNoOtherCalls();
        enrollments.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_RejectsEmptyCourseIdBeforeAccessingRepositories()
    {
        var courses = new Mock<ICourseRepository>(MockBehavior.Strict);
        var enrollments = new Mock<IEnrollmentRepository>(MockBehavior.Strict);
        var service = new EnrollmentService(enrollments.Object, courses.Object, new TestCurrentUser { Role = UserRole.Alumno });

        await Assert.ThrowsAsync<LumenValidationException>(() => service.CreateAsync(new EnrollmentCreateDTO(Guid.Empty)));
        courses.VerifyNoOtherCalls();
        enrollments.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_MissingCourseThrowsNotFoundWithoutInserting()
    {
        var courses = new Mock<ICourseRepository>();
        var enrollments = new Mock<IEnrollmentRepository>(MockBehavior.Strict);
        var service = new EnrollmentService(enrollments.Object, courses.Object, new TestCurrentUser { Role = UserRole.Alumno });

        await Assert.ThrowsAsync<CourseNotFoundException>(() => service.CreateAsync(new EnrollmentCreateDTO(Guid.NewGuid())));
        enrollments.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(CourseOperationalStatus.Pausado)]
    [InlineData(CourseOperationalStatus.Finalizado)]
    [InlineData(CourseOperationalStatus.Activo)]
    public async Task Create_ExistingUnavailableCourseThrowsConflictWithoutInserting(CourseOperationalStatus status)
    {
        var course = new Course { Id = Guid.NewGuid(), OperationalStatus = status };
        var courses = new Mock<ICourseRepository>();
        courses.Setup(x => x.GetByIdAsync(course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(course);
        var enrollments = new Mock<IEnrollmentRepository>(MockBehavior.Strict);
        var service = new EnrollmentService(enrollments.Object, courses.Object, new TestCurrentUser { Role = UserRole.Alumno });

        await Assert.ThrowsAsync<CourseStateConflictException>(() => service.CreateAsync(new EnrollmentCreateDTO(course.Id)));
        enrollments.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Create_FailedConditionalInsertDistinguishesUnavailableFromMissingCourse(bool missing)
    {
        var course = new Course { Id = Guid.NewGuid() };
        var courses = new Mock<ICourseRepository>();
        courses.Setup(x => x.GetPublishedByIdAsync(course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(course);
        courses.Setup(x => x.GetByIdAsync(course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(missing ? null : course);
        var enrollments = new Mock<IEnrollmentRepository>();
        var service = new EnrollmentService(enrollments.Object, courses.Object, new TestCurrentUser { Role = UserRole.Alumno });

        if (missing)
            await Assert.ThrowsAsync<CourseNotFoundException>(() => service.CreateAsync(new EnrollmentCreateDTO(course.Id)));
        else
            await Assert.ThrowsAsync<CourseStateConflictException>(() => service.CreateAsync(new EnrollmentCreateDTO(course.Id)));
    }

    [Fact]
    public async Task Create_PropagatesConcurrentDuplicateFromPersistence()
    {
        var course = new Course { Id = Guid.NewGuid() };
        var courses = new Mock<ICourseRepository>();
        courses.Setup(x => x.GetPublishedByIdAsync(course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(course);
        var enrollments = new Mock<IEnrollmentRepository>();
        enrollments.Setup(x => x.TryCreateForPublishedCourseAsync(
            It.IsAny<Enrollment>(), CourseOperationalStatus.Activo, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DuplicateEnrollmentException());
        var service = new EnrollmentService(enrollments.Object, courses.Object, new TestCurrentUser { Role = UserRole.Alumno });

        await Assert.ThrowsAsync<DuplicateEnrollmentException>(() => service.CreateAsync(new EnrollmentCreateDTO(course.Id)));
    }
}
