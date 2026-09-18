using Lumen.DataAccess;
using Lumen.DataAccess.Entities;
using Lumen.DataAccess.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var connectionString = args.FirstOrDefault(x => x.StartsWith("--connection=", StringComparison.OrdinalIgnoreCase))?
    .Split('=', 2)[1] ?? "Data Source=lumen.db";

var options = new DbContextOptionsBuilder<LumenDbContext>()
    .UseSqlite(connectionString, sqlite => sqlite.MigrationsAssembly("Lumen.Migrations"))
    .Options;

await using var context = new LumenDbContext(options);
var initializer = new DbInitializer(
    context,
    new UserRepository(context),
    new CourseRepository(context),
    new EnrollmentRepository(context),
    new PasswordHasher<User>());
await initializer.InitializeAsync();
var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
var userCount = await context.Users.CountAsync();
var courseCount = await context.Courses.CountAsync();
var enrollmentCount = await context.Enrollments.CountAsync();
Console.WriteLine($"Base de datos LUMEN inicializada: {connectionString}");
Console.WriteLine($"Migraciones aplicadas: {string.Join(", ", appliedMigrations)}");
Console.WriteLine($"Datos demo: {userCount} usuarios, {courseCount} cursos, {enrollmentCount} inscripciones.");
