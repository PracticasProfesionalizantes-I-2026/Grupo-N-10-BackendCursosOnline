using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Lumen.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lumen.API.IntegrationTests;

public sealed class LumenApiFactory : WebApplicationFactory<Program>
{
    private readonly string databasePath = Path.Combine(
        AppContext.BaseDirectory,
        $"lumen-integration-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<LumenDbContext>();
            services.RemoveAll<DbContextOptions<LumenDbContext>>();
            services.AddDbContext<LumenDbContext>(options =>
                options.UseSqlite(
                    $"Data Source={databasePath}",
                    sqlite => sqlite.MigrationsAssembly("Lumen.Migrations")));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }
}
