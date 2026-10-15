using Lumen.BusinessLogic.Services;
using Lumen.DataAccess.Entities;
using Lumen.DataAccess.Repositories;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Lumen.Shared.Exceptions;
using Moq;

namespace Lumen.BusinessLogic.Tests;

public sealed class CourseUpdateServiceTests
{
    [Theory]
    [InlineData(UserRole.Alumno, true)]
    [InlineData(UserRole.Profesor, false)]
    public async Task Update_RejectsUnauthorizedActorBeforeReading(UserRole role, bool authenticated)
    {
        var repository = new Mock<ICourseRepository>(MockBehavior.Strict);
        var service = new CourseService(repository.Object, new TestCurrentUser { Role = role, IsAuthenticated = authenticated });
        await Assert.ThrowsAsync<ForbiddenOperationException>(() => service.UpdateAsync(Guid.NewGuid(), ValidUpdate()));
    }

    [Fact]
    public async Task Update_RejectsForeignProfessorWithoutWrites()
    {
        var (course, repository) = SetupCourse(CourseRevisionStatus.Borrador);
        var service = new CourseService(repository.Object, new TestCurrentUser { Role = UserRole.Profesor });
        await Assert.ThrowsAsync<CourseOwnershipException>(() => service.UpdateAsync(course.Id, ValidUpdate()));
        VerifyNoWrites(repository);
    }

    [Theory]
    [InlineData(CourseRevisionStatus.Borrador, UserRole.Profesor)]
    [InlineData(CourseRevisionStatus.CambiosSolicitados, UserRole.Profesor)]
    [InlineData(CourseRevisionStatus.Borrador, UserRole.Administrador)]
    public async Task Update_BuildsValidatedChangesAndKeepsExistingDraftBehavior(CourseRevisionStatus status, UserRole role)
    {
        var (course, repository) = SetupCourse(status);
        CourseRevision? saved = null;
        repository.Setup(x => x.UpdateWorkingRevisionAsync(course, It.IsAny<CourseRevision>(), It.IsAny<CancellationToken>()))
            .Callback<Course, CourseRevision, CancellationToken>((_, changes, _) => saved = changes).Returns(Task.CompletedTask);
        await Service(course, repository, role).UpdateAsync(course.Id, ValidUpdate());

        Assert.NotNull(saved);
        Assert.Equal("Título nuevo", saved.Title);
        Assert.Equal("Descripción nueva", saved.Description);
        Assert.Equal("Categoría nueva", saved.Category);
        Assert.Equal("Avanzado", saved.Level);
        Assert.Equal("Presencial", saved.Modality);
        Assert.Equal(25, saved.MaxCapacity);
        Assert.Equal(["Objetivo"], saved.LearningObjectives);
        Assert.Empty(saved.SuggestedPrerequisites);
        Assert.Equal(CourseRevisionStatus.Borrador, saved.Status);
        Assert.Equal(30, saved.TotalDurationMinutes);
        Assert.True(course.UpdatedAtUtc > course.CreatedAtUtc);
        repository.Verify(x => x.CreateReviewAsync(It.IsAny<CourseReview>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(CourseRevisionStatus.EnRevision)]
    [InlineData(CourseRevisionStatus.Rechazado)]
    public async Task Update_RejectsNonEditableRevision(CourseRevisionStatus status)
    {
        var (course, repository) = SetupCourse(status);
        await Assert.ThrowsAsync<CourseStateConflictException>(() => Service(course, repository).UpdateAsync(course.Id, ValidUpdate()));
        VerifyNoWrites(repository);
    }

    [Theory]
    [InlineData(CourseRevisionStatus.Borrador, false)]
    [InlineData(CourseRevisionStatus.Publicado, false)]
    [InlineData(CourseRevisionStatus.Borrador, true)]
    [InlineData(CourseRevisionStatus.Publicado, true)]
    public async Task Update_RejectsDuplicateOrForeignIdsUniformly(CourseRevisionStatus status, bool foreign)
    {
        var (course, repository) = SetupCourse(status);
        var id = foreign ? Guid.NewGuid() : course.WorkingRevision!.Modules.Single().Id;
        var module = ValidUpdate().Modules.Single() with { Id = id };
        var update = ValidUpdate() with { Modules = foreign ? [module] : [module, module] };
        var before = course.UpdatedAtUtc;
        await Assert.ThrowsAsync<ModuleCourseMismatchException>(() => Service(course, repository).UpdateAsync(course.Id, update));
        Assert.Equal(before, course.UpdatedAtUtc);
        VerifyNoWrites(repository);
    }

    public static IEnumerable<object[]> InvalidUpdates()
    {
        yield return [null!];
        yield return [ValidUpdate() with { Modules = null! }];
        yield return [ValidUpdate() with { Modules = [null!] }];
        yield return [ValidUpdate() with { Title = new string('x', 201) }];
        yield return [ValidUpdate() with { Description = new string('x', 4001) }];
        yield return [ValidUpdate() with { Category = new string('x', 101) }];
        yield return [ValidUpdate() with { Level = new string('x', 101) }];
        yield return [ValidUpdate() with { Modality = new string('x', 101) }];
        yield return [ValidUpdate() with { MaxCapacity = -1 }];
        yield return [ValidUpdate() with { LearningObjectives = [null!] }];
        yield return [ValidUpdate() with { SuggestedPrerequisites = [null!] }];
        var module = ValidUpdate().Modules.Single();
        yield return [ValidUpdate() with { Modules = [module with { Name = new string('x', 201) }] }];
        yield return [ValidUpdate() with { Modules = [module with { Description = new string('x', 2001) }] }];
        yield return [ValidUpdate() with { Modules = [module with { Content = new string('x', 8001) }] }];
        yield return [ValidUpdate() with { Modules = [module with { Resources = [null!] }] }];
        yield return [ValidUpdate() with { Modules = [module with { DurationMinutes = 0 }] }];
        yield return [ValidUpdate() with { Modules = [module with { DurationMinutes = int.MaxValue }, module] }];
    }

    [Theory]
    [MemberData(nameof(InvalidUpdates))]
    public async Task Update_RejectsInvalidInputWithoutMutatingCourse(CourseUpdateDTO dto)
    {
        var (course, repository) = SetupCourse(CourseRevisionStatus.Borrador);
        var before = course.UpdatedAtUtc;
        await Assert.ThrowsAsync<LumenValidationException>(() => Service(course, repository).UpdateAsync(course.Id, dto));
        Assert.Equal(before, course.UpdatedAtUtc);
        Assert.Equal("Anterior", course.WorkingRevision!.Title);
        VerifyNoWrites(repository);
    }

    [Fact]
    public async Task Update_MissingCourseReturnsTypedError()
    {
        var repository = new Mock<ICourseRepository>();
        var service = new CourseService(repository.Object, new TestCurrentUser { Role = UserRole.Administrador });
        await Assert.ThrowsAsync<CourseNotFoundException>(() => service.UpdateAsync(Guid.NewGuid(), ValidUpdate()));
    }

    [Fact]
    public async Task Submit_CreatesModificationReviewAndAdvancesTimestampEvenWhenClockIsEarlier()
    {
        var (course, repository) = SetupCourse(CourseRevisionStatus.Borrador);
        course.PublishedRevisionId = Guid.NewGuid();
        course.UpdatedAtUtc = DateTime.UtcNow.AddDays(1);
        var previous = course.UpdatedAtUtc;
        CourseReview? requested = null;
        repository.Setup(x => x.CreateReviewAsync(It.IsAny<CourseReview>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CourseReview review, CancellationToken _) => { requested = review; return review; });

        await Service(course, repository).SubmitForReviewAsync(course.Id);
        Assert.Equal(CourseRevisionStatus.EnRevision, course.WorkingRevision!.Status);
        Assert.Equal(previous.AddTicks(1), course.UpdatedAtUtc);
        Assert.Equal(CourseReviewType.Modificacion, requested!.Type);
        Assert.Equal(course.WorkingRevisionId, requested.CourseRevisionId);
    }

    [Theory]
    [InlineData((CourseReviewDecision)99, null)]
    [InlineData(CourseReviewDecision.CambiosSolicitados, " ")]
    public async Task Resolve_RejectsInvalidDecisionBeforeReading(CourseReviewDecision decision, string? observation)
    {
        var repository = new Mock<ICourseRepository>(MockBehavior.Strict);
        var service = new CourseService(repository.Object, new TestCurrentUser { Role = UserRole.Administrador });
        await Assert.ThrowsAsync<LumenValidationException>(() => service.ResolveReviewAsync(Guid.NewGuid(), new(decision, observation)));
    }

    [Theory]
    [InlineData("title")]
    [InlineData("description")]
    [InlineData("category")]
    [InlineData("level")]
    [InlineData("modality")]
    [InlineData("capacity")]
    [InlineData("moduleName")]
    [InlineData("content")]
    [InlineData("length")]
    [InlineData("overflow")]
    public async Task Submit_RejectsIncompleteOrInvalidPersistedDataWithoutWrites(string invalid)
    {
        var (course, repository) = SetupCourse(CourseRevisionStatus.Borrador);
        var revision = course.WorkingRevision!;
        switch (invalid)
        {
            case "title": revision.Title = ""; break;
            case "description": revision.Description = " "; break;
            case "category": revision.Category = ""; break;
            case "level": revision.Level = ""; break;
            case "modality": revision.Modality = ""; break;
            case "capacity": revision.MaxCapacity = 0; break;
            case "moduleName": revision.Modules.Single().Name = ""; break;
            case "content": revision.Modules.Single().Content = ""; break;
            case "length": revision.Title = new string('x', 201); break;
            case "overflow":
                revision.Modules.Single().DurationMinutes = int.MaxValue;
                revision.Modules.Add(new() { Id = Guid.NewGuid(), Name = "Extra", Description = "Desc", DurationMinutes = 1, Content = "Contenido" });
                break;
        }
        await Assert.ThrowsAsync<LumenValidationException>(() => Service(course, repository).SubmitForReviewAsync(course.Id));
        Assert.Equal(CourseRevisionStatus.Borrador, revision.Status);
        repository.Verify(x => x.CreateReviewAsync(It.IsAny<CourseReview>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_RejectsVersionOverflowWithoutWrites()
    {
        var (course, repository) = SetupCourse(CourseRevisionStatus.Publicado);
        course.WorkingRevision!.Version = int.MaxValue;
        await Assert.ThrowsAsync<CourseStateConflictException>(() => Service(course, repository).UpdateAsync(course.Id, ValidUpdate()));
        VerifyNoWrites(repository);
    }

    private static CourseUpdateDTO ValidUpdate() => new(
        " Título nuevo ", "Descripción nueva", "Categoría nueva", "Avanzado", "Presencial", 25,
        [" Objetivo ", " "], [], [new(null, "Módulo nuevo", "Descripción", 30, "Contenido", [])]);

    private static (Course, Mock<ICourseRepository>) SetupCourse(CourseRevisionStatus status)
    {
        var teacher = Guid.NewGuid();
        var revision = new CourseRevision
        {
            Id = Guid.NewGuid(), Version = 1, Status = status, Title = "Anterior", Description = "Desc",
            Category = "Cat", Level = "Inicial", Modality = "Online", MaxCapacity = 10
        };
        revision.Modules.Add(new() { Id = Guid.NewGuid(), RevisionId = revision.Id, Order = 1,
            Name = "Módulo", Description = "Desc", DurationMinutes = 60, Content = "Contenido" });
        var course = new Course { Id = Guid.NewGuid(), OwnerProfessorId = teacher, CreatedByUserId = teacher,
            WorkingRevision = revision, WorkingRevisionId = revision.Id, CreatedAtUtc = DateTime.UtcNow.AddDays(-1) };
        revision.Course = course;
        var repository = new Mock<ICourseRepository>();
        repository.Setup(x => x.GetByIdForUpdateAsync(course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(course);
        return (course, repository);
    }

    private static CourseService Service(Course course, Mock<ICourseRepository> repository, UserRole role = UserRole.Profesor) =>
        new(repository.Object, new TestCurrentUser { UserId = course.OwnerProfessorId!.Value, Role = role });

    private static void VerifyNoWrites(Mock<ICourseRepository> repository)
    {
        repository.Verify(x => x.UpdateWorkingRevisionAsync(It.IsAny<Course>(), It.IsAny<CourseRevision>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.AddRevisionAsync(It.IsAny<Course>(), It.IsAny<CourseRevision>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

