using Lumen.BusinessLogic.Services;
using Lumen.DataAccess.Entities;
using Lumen.DataAccess.Repositories;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Lumen.Shared.Exceptions;
using Moq;

namespace Lumen.BusinessLogic.Tests;

public sealed class CourseServiceTests
{
    [Fact]
    public async Task UpdatePublishedCourse_CreatesWorkingRevisionAndPreservesPublishedRevision()
    {
        var teacherId = Guid.NewGuid();
        var published = PublishedRevision("Título público");
        var course = new Course
        {
            Id = Guid.NewGuid(),
            CreatedByUserId = teacherId,
            OwnerProfessorId = teacherId,
            WorkingRevisionId = published.Id,
            WorkingRevision = published,
            PublishedRevisionId = published.Id,
            PublishedRevision = published,
            OperationalStatus = CourseOperationalStatus.Activo
        };
        published.Course = course;
        published.CourseId = course.Id;
        var repository = new Mock<ICourseRepository>();
        repository.Setup(x => x.GetByIdForUpdateAsync(course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(course);
        repository.Setup(x => x.AddRevisionAsync(course, It.IsAny<CourseRevision>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Course _, CourseRevision revision, CancellationToken _) =>
            {
                revision.Id = Guid.NewGuid();
                course.WorkingRevision = revision;
                course.WorkingRevisionId = revision.Id;
                return revision;
            });
        var service = new CourseService(repository.Object, new TestCurrentUser { UserId = teacherId, Role = UserRole.Profesor });
        var moduleId = published.Modules.Single().Id;

        var result = await service.UpdateAsync(course.Id, new CourseUpdateDTO(
            "Título nuevo", "Descripción", "Categoría", "Nivel", "Online", 20,
            ["Objetivo"], ["Requisito"],
            [new CourseModuleUpdateDTO(moduleId, "Módulo", "Descripción", 60, "Nuevo contenido", [])]));

        Assert.Equal("Título público", course.PublishedRevision.Title);
        Assert.Equal("Título nuevo", course.WorkingRevision!.Title);
        Assert.Equal(1, result.PublishedVersion);
        Assert.Equal(2, result.WorkingVersion);
        Assert.NotEqual(course.PublishedRevisionId, course.WorkingRevisionId);
    }

    [Fact]
    public async Task SubmitForReview_RejectsCourseWithoutModules()
    {
        var teacherId = Guid.NewGuid();
        var revision = new CourseRevision
        {
            Id = Guid.NewGuid(), Version = 1, Status = CourseRevisionStatus.Borrador,
            Title = "Curso", Description = "Desc", Category = "Cat", Level = "Inicial",
            Modality = "Online", MaxCapacity = 10
        };
        var course = new Course { Id = Guid.NewGuid(), OwnerProfessorId = teacherId, WorkingRevision = revision };
        var repository = new Mock<ICourseRepository>();
        repository.Setup(x => x.GetByIdForUpdateAsync(course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(course);
        var service = new CourseService(repository.Object, new TestCurrentUser { UserId = teacherId, Role = UserRole.Profesor });

        await Assert.ThrowsAsync<CourseRequiresModuleException>(() => service.SubmitForReviewAsync(course.Id));
    }

    [Fact]
    public async Task PauseAsync_RejectsForeignTeacherCourse()
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            OwnerProfessorId = Guid.NewGuid(),
            PublishedRevisionId = Guid.NewGuid(),
            OperationalStatus = CourseOperationalStatus.Activo
        };
        var repository = new Mock<ICourseRepository>();
        repository.Setup(x => x.GetByIdForUpdateAsync(course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(course);
        var service = new CourseService(repository.Object, new TestCurrentUser { Role = UserRole.Profesor });

        await Assert.ThrowsAsync<CourseOwnershipException>(() => service.PauseAsync(course.Id));
    }

    private static CourseRevision PublishedRevision(string title)
    {
        var revision = new CourseRevision
        {
            Id = Guid.NewGuid(), Version = 1, Status = CourseRevisionStatus.Publicado,
            Title = title, Description = "Descripción", Category = "Categoría", Level = "Inicial",
            Modality = "Online", MaxCapacity = 20, TotalDurationMinutes = 60,
            CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow, PublishedAtUtc = DateTime.UtcNow
        };
        revision.Modules.Add(new CourseModule
        {
            Id = Guid.NewGuid(), RevisionId = revision.Id, Order = 1, Name = "Módulo",
            Description = "Descripción", DurationMinutes = 60, Content = "Contenido"
        });
        return revision;
    }
}

