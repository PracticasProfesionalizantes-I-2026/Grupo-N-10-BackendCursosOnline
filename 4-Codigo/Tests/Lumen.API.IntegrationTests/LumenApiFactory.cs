using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Lumen.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Lumen.API.IntegrationTests;

public sealed class LumenApiFactory(params IInterceptor[] interceptors) : WebApplicationFactory<Program>
{
    private readonly string databasePath = Path.Combine(
        Path.GetTempPath(),
        $"lumen-integration-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders().AddConsole());
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<LumenDbContext>();
            services.RemoveAll<DbContextOptions<LumenDbContext>>();
            services.AddDbContext<LumenDbContext>(options =>
                options.UseSqlite(
                    $"Data Source={databasePath};Pooling=False",
                    sqlite => sqlite.MigrationsAssembly("Lumen.Migrations"))
                    .AddInterceptors(interceptors));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        foreach (var path in new[] { databasePath, databasePath + "-wal", databasePath + "-shm", databasePath + "-journal" })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
