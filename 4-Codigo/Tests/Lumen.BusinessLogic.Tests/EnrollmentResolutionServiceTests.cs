using Lumen.BusinessLogic.Services;
using Lumen.DataAccess.Entities;
using Lumen.DataAccess.Repositories;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Lumen.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Lumen.BusinessLogic.Tests;

public sealed class EnrollmentResolutionServiceTests
{
    [Theory]
    [InlineData(EnrollmentStatus.Aprobada)]
    [InlineData(EnrollmentStatus.Rechazada)]
    public async Task Resolve_ConditionallySavesPendingResolutionAndMapsResponse(EnrollmentStatus decision)
    {
        var administrator = new TestCurrentUser { Role = UserRole.Administrador };
        var enrollment = PendingEnrollment();
        var repository = ReadRepository(enrollment);
        using var cancellation = new CancellationTokenSource();
        var before = DateTime.UtcNow;
        DateTime persistedAt = default;
        repository.Setup(x => x.TryResolveAsync(enrollment.Id, EnrollmentStatus.Pendiente, decision,
                It.IsAny<DateTime>(), administrator.UserId, cancellation.Token))
            .Callback<Guid, EnrollmentStatus, EnrollmentStatus, DateTime, Guid, CancellationToken>((_, _, _, at, _, _) =>
            {
                Assert.Equal(EnrollmentStatus.Pendiente, enrollment.Status);
                Assert.Null(enrollment.ResolvedAtUtc);
                persistedAt = at;
            }).ReturnsAsync(true);
        var service = new EnrollmentService(repository.Object, Mock.Of<ICourseRepository>(), administrator);

        var result = await service.ResolveAsync(enrollment.Id, new EnrollmentDecisionDTO(decision), cancellation.Token);

        Assert.Equal(enrollment.Id, result.Id);
        Assert.Equal(decision, result.Status);
        Assert.Equal(persistedAt, result.ResolvedAtUtc);
        Assert.InRange(persistedAt, before, DateTime.UtcNow);
        Assert.Equal(administrator.UserId, enrollment.ResolvedByUserId);
        Assert.Null(enrollment.CanceledAtUtc);
        Assert.Null(enrollment.CanceledByUserId);
        repository.Verify(x => x.TryResolveAsync(enrollment.Id, EnrollmentStatus.Pendiente, decision,
            persistedAt, administrator.UserId, cancellation.Token), Times.Once);
        VerifyNoTrackedSave(repository);
    }

    [Theory]
    [InlineData(EnrollmentStatus.Aprobada)]
    [InlineData(EnrollmentStatus.Rechazada)]
    public async Task Resolve_ConcurrentStateChangeThrowsConflictWithoutMutatingReadSnapshot(EnrollmentStatus decision)
    {
        var enrollment = PendingEnrollment();
        var repository = ReadRepository(enrollment);
        var service = new EnrollmentService(repository.Object, Mock.Of<ICourseRepository>(),
            new TestCurrentUser { Role = UserRole.Administrador });

        await Assert.ThrowsAsync<EnrollmentStateConflictException>(() => service.ResolveAsync(enrollment.Id, new EnrollmentDecisionDTO(decision)));

        Assert.Equal(EnrollmentStatus.Pendiente, enrollment.Status);
        Assert.Null(enrollment.ResolvedAtUtc);
        Assert.Null(enrollment.ResolvedByUserId);
        VerifyNoTrackedSave(repository);
    }

    [Theory]
    [InlineData(EnrollmentStatus.Aprobada)]
    [InlineData(EnrollmentStatus.Rechazada)]
    [InlineData(EnrollmentStatus.Cancelada)]
    public async Task Resolve_AlreadyResolvedEnrollmentThrowsConflictBeforeWriting(EnrollmentStatus currentStatus)
    {
        var enrollment = PendingEnrollment();
        enrollment.Status = currentStatus;
        var repository = ReadRepository(enrollment);
        var service = new EnrollmentService(repository.Object, Mock.Of<ICourseRepository>(),
            new TestCurrentUser { Role = UserRole.Administrador });

        await Assert.ThrowsAsync<EnrollmentStateConflictException>(() => service.ResolveAsync(enrollment.Id, new EnrollmentDecisionDTO(EnrollmentStatus.Aprobada)));

        repository.Verify(x => x.TryResolveAsync(It.IsAny<Guid>(), It.IsAny<EnrollmentStatus>(), It.IsAny<EnrollmentStatus>(),
            It.IsAny<DateTime>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNoTrackedSave(repository);
    }

    [Theory]
    [InlineData(EnrollmentStatus.Pendiente)]
    [InlineData(EnrollmentStatus.Cancelada)]
    public async Task Resolve_InvalidDecisionIsRejectedBeforeAccessingRepository(EnrollmentStatus decision)
    {
        var repository = new Mock<IEnrollmentRepository>(MockBehavior.Strict);
        var service = new EnrollmentService(repository.Object, Mock.Of<ICourseRepository>(),
            new TestCurrentUser { Role = UserRole.Administrador });

        await Assert.ThrowsAsync<LumenValidationException>(() => service.ResolveAsync(Guid.NewGuid(), new EnrollmentDecisionDTO(decision)));
        repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(UserRole.Alumno, true)]
    [InlineData(UserRole.Profesor, true)]
    [InlineData(UserRole.Administrador, false)]
    public async Task Resolve_UnauthorizedActorIsRejectedBeforeAccessingRepository(UserRole role, bool authenticated)
    {
        var repository = new Mock<IEnrollmentRepository>(MockBehavior.Strict);
        var service = new EnrollmentService(repository.Object, Mock.Of<ICourseRepository>(),
            new TestCurrentUser { Role = role, IsAuthenticated = authenticated });

        await Assert.ThrowsAsync<ForbiddenOperationException>(() => service.ResolveAsync(Guid.NewGuid(), new EnrollmentDecisionDTO(EnrollmentStatus.Aprobada)));
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Resolve_MissingEnrollmentThrowsNotFound()
    {
        var repository = new Mock<IEnrollmentRepository>();
        var service = new EnrollmentService(repository.Object, Mock.Of<ICourseRepository>(),
            new TestCurrentUser { Role = UserRole.Administrador });

        await Assert.ThrowsAsync<EnrollmentNotFoundException>(() => service.ResolveAsync(Guid.NewGuid(), new EnrollmentDecisionDTO(EnrollmentStatus.Aprobada)));
        VerifyNoTrackedSave(repository);
    }

    [Fact]
    public async Task Resolve_UnrelatedPersistenceExceptionIsNotConvertedToBusinessConflict()
    {
        var enrollment = PendingEnrollment();
        var repository = ReadRepository(enrollment);
        var failure = new DbUpdateException("Fallo de persistencia ajeno al estado de la inscripción.");
        repository.Setup(x => x.TryResolveAsync(It.IsAny<Guid>(), It.IsAny<EnrollmentStatus>(), It.IsAny<EnrollmentStatus>(),
            It.IsAny<DateTime>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ThrowsAsync(failure);
        var service = new EnrollmentService(repository.Object, Mock.Of<ICourseRepository>(),
            new TestCurrentUser { Role = UserRole.Administrador });

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => service.ResolveAsync(enrollment.Id, new EnrollmentDecisionDTO(EnrollmentStatus.Aprobada)));

        Assert.Same(failure, error);
        Assert.Equal(EnrollmentStatus.Pendiente, enrollment.Status);
        VerifyNoTrackedSave(repository);
    }

    private static Enrollment PendingEnrollment() => new()
    {
        Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), CourseId = Guid.NewGuid(),
        Status = EnrollmentStatus.Pendiente, RequestedAtUtc = DateTime.UtcNow,
        Course = new Course { PublishedRevision = new CourseRevision { Title = "Curso publicado" } }
    };

    private static Mock<IEnrollmentRepository> ReadRepository(Enrollment enrollment)
    {
        var repository = new Mock<IEnrollmentRepository>();
        repository.Setup(x => x.GetByIdAsync(enrollment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(enrollment);
        return repository;
    }

    private static void VerifyNoTrackedSave(Mock<IEnrollmentRepository> repository)
    {
        repository.Verify(x => x.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
