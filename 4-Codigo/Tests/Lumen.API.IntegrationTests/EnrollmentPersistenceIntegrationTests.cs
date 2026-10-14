using Lumen.DataAccess;
using Lumen.DataAccess.Entities;
using Lumen.DataAccess.Repositories;
using Lumen.Shared.Enums;
using Lumen.Shared.Exceptions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.API.IntegrationTests;

public sealed class EnrollmentPersistenceIntegrationTests
{
    [Theory]
    [InlineData(EnrollmentStatus.Pendiente, EnrollmentStatus.Pendiente)]
    [InlineData(EnrollmentStatus.Pendiente, EnrollmentStatus.Aprobada)]
    [InlineData(EnrollmentStatus.Aprobada, EnrollmentStatus.Pendiente)]
    [InlineData(EnrollmentStatus.Aprobada, EnrollmentStatus.Aprobada)]
    public async Task Database_RejectsAnySecondPendingOrApprovedForSamePair(
        EnrollmentStatus existingStatus, EnrollmentStatus newStatus)
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        var existing = await context.Enrollments.SingleAsync(x => x.Status == existingStatus);
        var before = await context.Enrollments.CountAsync();
        context.Enrollments.Add(new Enrollment
        {
            Id = Guid.NewGuid(), StudentId = existing.StudentId, CourseId = existing.CourseId,
            Status = newStatus, RequestedAtUtc = DateTime.UtcNow
        });

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        Assert.Equal(2067, Assert.IsType<SqliteException>(error.InnerException).SqliteExtendedErrorCode);
        context.ChangeTracker.Clear();
        Assert.Equal(before, await context.Enrollments.CountAsync());
        Assert.Equal(existingStatus, (await context.Enrollments.SingleAsync(x => x.Id == existing.Id)).Status);
    }

    [Theory]
    [InlineData(EnrollmentStatus.Pendiente, EnrollmentStatus.Pendiente)]
    [InlineData(EnrollmentStatus.Pendiente, EnrollmentStatus.Aprobada)]
    [InlineData(EnrollmentStatus.Aprobada, EnrollmentStatus.Pendiente)]
    [InlineData(EnrollmentStatus.Aprobada, EnrollmentStatus.Aprobada)]
    public async Task Repository_ConcurrentInsertsEnforceUniquenessWithoutServicePrecheck(
        EnrollmentStatus firstStatus, EnrollmentStatus secondStatus)
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        Guid courseId;
        var studentId = Guid.NewGuid();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
            courseId = await context.Courses.OrderBy(x => x.Id).Select(x => x.Id).FirstAsync();
            context.Users.Add(new User { Id = studentId, Email = $"{studentId}@lumen.local", NormalizedEmail = studentId.ToString(), Role = UserRole.Alumno });
            await context.SaveChangesAsync();
        }
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = new[] { firstStatus, secondStatus }.Select(status => Task.Run(async () =>
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IEnrollmentRepository>();
            await start.Task;
            try
            {
                return await repository.TryCreateForPublishedCourseAsync(new Enrollment
                {
                    StudentId = studentId, CourseId = courseId, Status = status, RequestedAtUtc = DateTime.UtcNow
                }, CourseOperationalStatus.Activo);
            }
            catch (DuplicateEnrollmentException) { return false; }
        })).ToArray();
        start.SetResult();

        var results = await Task.WhenAll(tasks);

        Assert.Single(results, x => x);
        await using var verification = factory.Services.CreateAsyncScope();
        var persisted = await verification.ServiceProvider.GetRequiredService<LumenDbContext>().Enrollments
            .Where(x => x.StudentId == studentId && x.CourseId == courseId).ToListAsync();
        Assert.Single(persisted);
    }

    [Fact]
    public async Task Repository_DoesNotTranslateUnrelatedDatabaseFailureAsDuplicate()
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        var courseId = await context.Courses.OrderBy(x => x.Id).Select(x => x.Id).FirstAsync();
        var before = await context.Enrollments.CountAsync();
        var repository = scope.ServiceProvider.GetRequiredService<IEnrollmentRepository>();

        var error = await Assert.ThrowsAsync<SqliteException>(() => repository.TryCreateForPublishedCourseAsync(
            new Enrollment { StudentId = Guid.NewGuid(), CourseId = courseId, Status = EnrollmentStatus.Pendiente, RequestedAtUtc = DateTime.UtcNow },
            CourseOperationalStatus.Activo));

        Assert.Equal(787, error.SqliteExtendedErrorCode);
        Assert.Equal(before, await context.Enrollments.CountAsync());
    }

    [Fact]
    public async Task Migration_PreservesHistoryAndAllowsPendingToApprovedToCanceledThenNewRequest()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        await context.GetService<IMigrator>().MigrateAsync("20260910232809_InitialCreate");
        var rows = await AddLegacyEnrollmentsAsync(context,
            EnrollmentStatus.Rechazada, EnrollmentStatus.Rechazada, EnrollmentStatus.Cancelada, EnrollmentStatus.Cancelada, EnrollmentStatus.Pendiente);
        var originalIds = rows.Select(x => x.Id).Order().ToArray();

        await context.Database.MigrateAsync();

        Assert.Equal(originalIds, await context.Enrollments.OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync());
        var active = rows.Single(x => x.Status == EnrollmentStatus.Pendiente);
        active.Status = EnrollmentStatus.Aprobada;
        await context.SaveChangesAsync();
        active.Status = EnrollmentStatus.Cancelada;
        await context.SaveChangesAsync();
        var replacement = new Enrollment
        {
            Id = Guid.NewGuid(), StudentId = active.StudentId, CourseId = active.CourseId,
            Status = EnrollmentStatus.Pendiente, RequestedAtUtc = DateTime.UtcNow
        };
        context.Enrollments.Add(replacement);
        await context.SaveChangesAsync();
        Assert.Equal(6, await context.Enrollments.CountAsync());
        Assert.Equal(1, await context.Enrollments.CountAsync(x => x.Status == EnrollmentStatus.Pendiente || x.Status == EnrollmentStatus.Aprobada));
        context.Enrollments.Add(new Enrollment
        {
            Id = Guid.NewGuid(), StudentId = active.StudentId, CourseId = active.CourseId,
            Status = EnrollmentStatus.Aprobada, RequestedAtUtc = DateTime.UtcNow
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Theory]
    [InlineData(EnrollmentStatus.Pendiente, EnrollmentStatus.Pendiente)]
    [InlineData(EnrollmentStatus.Pendiente, EnrollmentStatus.Aprobada)]
    public async Task Migration_ExistingActiveDuplicatesFailWithoutDeletingOrChangingHistory(
        EnrollmentStatus first, EnrollmentStatus second)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        await context.GetService<IMigrator>().MigrateAsync("20260910232809_InitialCreate");
        var rows = await AddLegacyEnrollmentsAsync(context, first, second);
        var before = rows.Select(x => (x.Id, x.Status, x.RequestedAtUtc)).OrderBy(x => x.Id).ToArray();

        var error = await Assert.ThrowsAsync<SqliteException>(() => context.Database.MigrateAsync());

        Assert.Equal(2067, error.SqliteExtendedErrorCode);
        context.ChangeTracker.Clear();
        var after = (await context.Enrollments.AsNoTracking().ToListAsync())
            .Select(x => (x.Id, x.Status, x.RequestedAtUtc)).OrderBy(x => x.Id).ToArray();
        Assert.Equal(before, after);
        Assert.Equal(new[] { "20260910232809_InitialCreate" }, await context.Database.GetAppliedMigrationsAsync());
    }

    private static LumenDbContext CreateContext(SqliteConnection connection) => new(
        new DbContextOptionsBuilder<LumenDbContext>().UseSqlite(connection,
            sqlite => sqlite.MigrationsAssembly("Lumen.Migrations")).Options);

    private static async Task<Enrollment[]> AddLegacyEnrollmentsAsync(LumenDbContext context, params EnrollmentStatus[] statuses)
    {
        var student = new User { Id = Guid.NewGuid(), Email = "legacy@lumen.local", NormalizedEmail = "LEGACY@LUMEN.LOCAL", Role = UserRole.Alumno };
        var course = new Course { Id = Guid.NewGuid(), CreatedByUserId = student.Id };
        context.Users.Add(student);
        context.Courses.Add(course);
        await context.SaveChangesAsync();
        var rows = statuses.Select(status => new Enrollment
        {
            Id = Guid.NewGuid(), StudentId = student.Id, CourseId = course.Id, Status = status, RequestedAtUtc = DateTime.UtcNow
        }).ToArray();
        context.Enrollments.AddRange(rows);
        await context.SaveChangesAsync();
        return rows;
    }
}
