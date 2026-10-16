using Lumen.DataAccess;
using Lumen.DataAccess.Entities;
using Lumen.DataAccess.Repositories;
using Lumen.Shared.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Lumen.API.IntegrationTests.ConcurrencyTestSupport;

namespace Lumen.API.IntegrationTests;

public sealed class ConcurrencyPersistenceFailureIntegrationTests
{
    [Fact]
    public async Task CU16_UnrelatedInsertFailure_PropagatesWithoutRecordingCompletion()
    {
        using var factory = new LumenApiFactory();
        using var student = factory.CreateClient();
        await AuthenticateAsync(student, "alumno@lumen.local");
        var scenario = await PrepareAsync(factory);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
            // This trigger exists only in this factory's temporary database.
            await context.Database.ExecuteSqlRawAsync("""
                CREATE TRIGGER "RejectCompletionForTest"
                BEFORE INSERT ON "EnrollmentModuleCompletions"
                BEGIN
                    SELECT RAISE(ABORT, 'test-only completion failure');
                END;
                """);
        }

        using var outcome = await CaptureAsync(() => student.PutAsync(CompletionUrl(scenario, scenario.ModuleIds[0]), null));
        var saved = await ReadAsync(factory, scenario);

        Assert.Multiple(
            () => Assert.Null(outcome.Response),
            () => Assert.Equal(1811, Assert.IsType<SqliteException>(outcome.Error).SqliteExtendedErrorCode),
            () => Assert.Equal(EnrollmentStatus.Aprobada, saved.Status),
            () => Assert.Equal(scenario.PublicationId, saved.PublicationId),
            () => Assert.Empty(saved.Completions));
    }

    [Fact]
    public async Task CU01_UnrelatedNotNullFailure_IsNotTranslatedToDuplicateEmail()
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var before = await context.Users.CountAsync();

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => repository.CreateAsync(new User
        {
            Email = "not-null-failure@lumen.local",
            NormalizedEmail = null!,
            Role = UserRole.Alumno
        }));

        Assert.Equal(1299, Assert.IsType<SqliteException>(error.InnerException).SqliteExtendedErrorCode);
        Assert.Equal(before, await context.Users.AsNoTracking().CountAsync());
        Assert.False(await context.Users.AsNoTracking().AnyAsync(x => x.Email == "not-null-failure@lumen.local"));
    }
}
