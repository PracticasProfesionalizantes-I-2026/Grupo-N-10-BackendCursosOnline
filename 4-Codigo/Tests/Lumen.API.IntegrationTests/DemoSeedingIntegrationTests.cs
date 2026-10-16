using System.Net;
using System.Net.Http.Json;
using Lumen.DataAccess;
using Lumen.DataAccess.Entities;
using Lumen.DataAccess.Repositories;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static Lumen.API.IntegrationTests.ConcurrencyTestSupport;

namespace Lumen.API.IntegrationTests;

public sealed class DemoSeedingIntegrationTests
{
    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task RestrictedEnvironment_StartsAndMigratesEmptyDatabase_WithoutDemoData(string environment)
    {
        using var root = new LumenApiFactory();
        using var factory = root.WithWebHostBuilder(builder => builder.UseEnvironment(environment));
        Assert.False(File.Exists(root.DatabasePath));
        using var client = factory.CreateClient();
        Assert.Equal(environment, factory.Services.GetRequiredService<IHostEnvironment>().EnvironmentName);
        using var openApi = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, openApi.StatusCode);
        using var scalar = await client.GetAsync("/scalar/v1");
        Assert.Equal(HttpStatusCode.OK, scalar.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        Assert.Equal(Path.GetFullPath(root.DatabasePath), Path.GetFullPath(context.Database.GetDbConnection().DataSource));
        await AssertMigratedAndEmptyAsync(context);
        await scope.ServiceProvider.GetRequiredService<DbInitializer>().InitializeAsync(environment);
        await AssertMigratedAndEmptyAsync(context);
        foreach (var email in new[] { "admin@lumen.local", "profesor@lumen.local", "alumno@lumen.local" })
        {
            using var login = await client.PostAsJsonAsync("/api/auth/login",
                new LoginRequestDTO(email, DbInitializer.DemoPassword), Json);
            Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
            var error = await login.Content.ReadFromJsonAsync<ApiErrorResponseDTO>(Json);
            Assert.Equal("InvalidCredentialsException", error?.Code);
        }
        await AssertMigratedAndEmptyAsync(context);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    public async Task PermittedEnvironment_SeedsExistingDemoDataAndCredentials_AndRemainsIdempotent(string environment)
    {
        using var root = new LumenApiFactory();
        using var factory = root.WithWebHostBuilder(builder => builder.UseEnvironment(environment));
        using var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        var before = await ReadUsersAsync(context);
        Assert.Equal(3, before.Length);
        Assert.Equal(2, await context.Courses.CountAsync());
        Assert.Equal(2, await context.CourseRevisions.CountAsync());
        Assert.Equal(5, await context.CourseModules.CountAsync());
        Assert.Equal(2, await context.Enrollments.CountAsync());
        Assert.Equal(1, await context.EnrollmentModuleCompletions.CountAsync());
        var courseIds = await context.Courses.AsNoTracking().OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync();
        var enrollmentIds = await context.Enrollments.AsNoTracking().OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync();

        var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
        await initializer.InitializeAsync(environment);
        await initializer.InitializeAsync(environment);
        Assert.Equal(before, await ReadUsersAsync(context));
        Assert.Equal(courseIds, await context.Courses.AsNoTracking().OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync());
        Assert.Equal(enrollmentIds, await context.Enrollments.AsNoTracking().OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync());
        foreach (var (email, role) in new[]
        {
            ("alumno@lumen.local", UserRole.Alumno),
            ("profesor@lumen.local", UserRole.Profesor),
            ("admin@lumen.local", UserRole.Administrador)
        })
        {
            using var login = await client.PostAsJsonAsync("/api/auth/login",
                new LoginRequestDTO(email, DbInitializer.DemoPassword), Json);
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            var authenticated = await login.Content.ReadFromJsonAsync<LoginResponseDTO>(Json);
            Assert.NotNull(authenticated);
            Assert.Equal(role, authenticated.User.Role);
            Assert.False(string.IsNullOrWhiteSpace(authenticated.AccessToken));
        }
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Development")]
    [InlineData("Testing")]
    public async Task ExistingUsers_AreNeverModifiedOrDeleted_AndDoNotTriggerDemoSeeding(string environment)
    {
        using var root = new LumenApiFactory();
        UserState[] before;
        await using (var preparation = CreateContext(root.DatabasePath))
        {
            await preparation.Database.MigrateAsync();
            preparation.Users.AddRange(
                ExistingUser("existing@example.test", UserRole.Profesor, isActive: false),
                // Even an existing account with a demo email must not be deleted or rewritten.
                ExistingUser("admin@lumen.local", UserRole.Administrador, isActive: true));
            await preparation.SaveChangesAsync();
            before = await ReadUsersAsync(preparation);
        }

        using var factory = root.WithWebHostBuilder(builder => builder.UseEnvironment(environment));
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        Assert.Equal(before, await ReadUsersAsync(context));
        await scope.ServiceProvider.GetRequiredService<DbInitializer>().InitializeAsync(environment);
        Assert.Equal(before, await ReadUsersAsync(context));
        Assert.Equal(0, await context.Courses.CountAsync());
        Assert.Equal(0, await context.Enrollments.CountAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Preview")]
    public async Task Initializer_WithoutAnExplicitPermittedEnvironment_MigratesButDoesNotSeed(string? environment)
    {
        using var owner = new LumenApiFactory();
        await using var context = CreateContext(owner.DatabasePath);
        await Initializer(context).InitializeAsync(environment);
        await AssertMigratedAndEmptyAsync(context);
    }

    [Fact]
    public async Task Initializer_DefaultCall_DoesNotSeedDemoData()
    {
        using var owner = new LumenApiFactory();
        await using var context = CreateContext(owner.DatabasePath);
        await Initializer(context).InitializeAsync();
        await AssertMigratedAndEmptyAsync(context);
    }

    [Fact]
    public async Task Production_PublicRegistrationWorksWithoutAnyDemoAccount()
    {
        using var root = new LumenApiFactory();
        using var factory = root.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/auth/register", new UserCreateDTO(
            "first@example.test", DbInitializer.DemoPassword, UserRole.Alumno, "Primero", "Real",
            "12345678", "123456", "Dirección", "1000"), Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        var user = Assert.Single(await context.Users.AsNoTracking().ToListAsync());
        Assert.Equal("first@example.test", user.Email);
        Assert.Equal(UserRole.Alumno, user.Role);
        Assert.Equal(0, await context.Courses.CountAsync());
        Assert.Equal(0, await context.Enrollments.CountAsync());
        using var login = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDTO(user.Email, DbInitializer.DemoPassword), Json);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    private static LumenDbContext CreateContext(string path) => new(new DbContextOptionsBuilder<LumenDbContext>()
        .UseSqlite($"Data Source={path};Pooling=False", options => options.MigrationsAssembly("Lumen.Migrations"))
        .Options);

    private static DbInitializer Initializer(LumenDbContext context) => new(context, new UserRepository(context),
        new CourseRepository(context), new EnrollmentRepository(context), new PasswordHasher<User>());

    private static async Task AssertMigratedAndEmptyAsync(LumenDbContext context)
    {
        Assert.NotEmpty(await context.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.Equal(0, await context.Users.CountAsync());
        Assert.Equal(0, await context.Courses.CountAsync());
        Assert.Equal(0, await context.CourseRevisions.CountAsync());
        Assert.Equal(0, await context.CourseModules.CountAsync());
        Assert.Equal(0, await context.CourseReviews.CountAsync());
        Assert.Equal(0, await context.Enrollments.CountAsync());
        Assert.Equal(0, await context.EnrollmentModuleCompletions.CountAsync());
    }

    private static User ExistingUser(string email, UserRole role, bool isActive) => new()
    {
        Id = Guid.NewGuid(), Email = email, NormalizedEmail = email.ToUpperInvariant(),
        PasswordHash = "existing-test-hash-do-not-change", Role = role, FirstName = "Existente", LastName = "Usuario",
        Dni = "87654321", Phone = "123456", Address = "Dirección previa", PostalCode = "1000",
        IsActive = isActive, CreatedAtUtc = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc)
    };

    private static async Task<UserState[]> ReadUsersAsync(LumenDbContext context) =>
        (await context.Users.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync()).Select(x => new UserState(
            x.Id, x.Email, x.NormalizedEmail, x.PasswordHash, x.Role, x.FirstName, x.LastName, x.Dni,
            x.Phone, x.Address, x.PostalCode, x.IsActive, x.CreatedAtUtc)).ToArray();

    private sealed record UserState(Guid Id, string Email, string NormalizedEmail, string PasswordHash,
        UserRole Role, string FirstName, string LastName, string Dni, string Phone, string Address,
        string PostalCode, bool IsActive, DateTime CreatedAtUtc);
}
