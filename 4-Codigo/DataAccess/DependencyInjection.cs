using Lumen.DataAccess.Entities;
using Lumen.DataAccess.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.DataAccess;

public static class DependencyInjection
{
    public static IServiceCollection AddDataAccess(
        this IServiceCollection services,
        string connectionString,
        string migrationsAssembly = "Lumen.Migrations")
    {
        services.AddDbContext<LumenDbContext>(options =>
            options.UseSqlite(connectionString, sqlite => sqlite.MigrationsAssembly(migrationsAssembly)));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<DbInitializer>();
        return services;
    }
}
