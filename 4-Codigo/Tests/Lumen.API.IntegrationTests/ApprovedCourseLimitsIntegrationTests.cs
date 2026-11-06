using System.Net;
using System.Net.Http.Json;
using Lumen.DataAccess;
using Lumen.DataAccess.Entities;
using Lumen.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.API.IntegrationTests;

public sealed class ApprovedCourseLimitsIntegrationTests
{
    [Theory]
    [InlineData("modules")]
    [InlineData("objectives")]
    [InlineData("prerequisites")]
    [InlineData("resources")]
    [InlineData("objectiveLength")]
    [InlineData("prerequisiteLength")]
    [InlineData("resourceLength")]
    public async Task NewCourse_AcceptsBoundaryAndRejectsOneOverWithoutWriting(string field)
    {
        using var factory = new LumenApiFactory();
        using var teacher = await CourseTestClient.LoginAsync(factory);
        var accepted = Sample(field, exceeded: false);
        using var valid = await teacher.PostAsJsonAsync("/api/courses", accepted, CourseTestClient.Json);
        Assert.Equal(HttpStatusCode.Created, valid.StatusCode);
        var course = (await valid.Content.ReadFromJsonAsync<CourseResponseDTO>(CourseTestClient.Json))!;
        using var sent = await teacher.PostAsync($"/api/courses/{course.Id}/submit-review", null);
        Assert.Equal(HttpStatusCode.OK, sent.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        var before = await db.Courses.CountAsync();
        using var invalid = await teacher.PostAsJsonAsync("/api/courses", Sample(field, exceeded: true), CourseTestClient.Json);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(before, await db.Courses.CountAsync());
    }

    [Fact]
    public async Task LegacyOversizeDraft_CanShrinkButCannotGrow()
    {
        using var factory = new LumenApiFactory();
        using var teacher = await CourseTestClient.LoginAsync(factory);
        var draft = await CourseTestClient.DraftAsync(teacher);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
            var revisionId = await db.Courses.Where(x => x.Id == draft.Id).Select(x => x.WorkingRevisionId).SingleAsync();
            for (var index = 4; index <= 101; index++)
                db.CourseModules.Add(new CourseModule
                {
                    Id = Guid.NewGuid(), RevisionId = revisionId!.Value, Order = index,
                    Name = $"Legado {index}", Description = "Desc", DurationMinutes = 1, Content = "Contenido"
                });
            await db.SaveChangesAsync();
        }
        var legacy = await CourseTestClient.GetAsync(teacher, draft.Id);
        Assert.Equal(101, legacy.Modules.Count);
        var unchanged = await CourseTestClient.PutAsync(teacher, draft.Id, CourseTestClient.Update(legacy));
        Assert.Equal(101, unchanged.Modules.Count);
        var oneMore = CourseTestClient.Update(unchanged) with
        {
            Modules = [.. unchanged.Modules.Select(CourseTestClient.Module),
                new CourseModuleUpdateDTO(null, "Nuevo", "Desc", 1, "Contenido", [])]
        };
        using var rejected = await CourseTestClient.SendEditAsync(teacher, draft.Id, oneMore,
            await CourseTestClient.ReadTagAsync(teacher, draft.Id));
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        var reduced = CourseTestClient.Update(unchanged) with
        {
            Modules = unchanged.Modules.Take(100).Select(CourseTestClient.Module).ToList()
        };
        var saved = await CourseTestClient.PutAsync(teacher, draft.Id, reduced);
        Assert.Equal(100, saved.Modules.Count);
    }

    private static CourseCreateDTO Sample(string field, bool exceeded)
    {
        var count = exceeded ? 1 : 0;
        var module = new CourseModuleCreateDTO("Módulo", "Desc", 1, "Contenido", []);
        var dto = new CourseCreateDTO("Título", "Descripción", "Categoría", "Inicial", "Online", 1,
            [], [], [module]);
        return field switch
        {
            "modules" => dto with { Modules = Enumerable.Repeat(module, 100 + count).ToArray() },
            "objectives" => dto with { LearningObjectives = Enumerable.Repeat("Objetivo", 30 + count).ToArray() },
            "prerequisites" => dto with { SuggestedPrerequisites = Enumerable.Repeat("Requisito", 30 + count).ToArray() },
            "resources" => dto with { Modules = [module with { Resources = Enumerable.Repeat("Recurso", 50 + count).ToArray() }] },
            "objectiveLength" => dto with { LearningObjectives = [new string('A', 500 + count)] },
            "prerequisiteLength" => dto with { SuggestedPrerequisites = [new string('A', 500 + count)] },
            "resourceLength" => dto with { Modules = [module with { Resources = [new string('A', 2000 + count)] }] },
            _ => throw new ArgumentOutOfRangeException(nameof(field))
        };
    }
}
