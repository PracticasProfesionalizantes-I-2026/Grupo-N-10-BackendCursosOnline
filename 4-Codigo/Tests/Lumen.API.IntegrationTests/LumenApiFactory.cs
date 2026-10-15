using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Lumen.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Lumen.API.IntegrationTests;

public sealed class LumenApiFactory : WebApplicationFactory<Program>
{
    private readonly IInterceptor[] interceptors;
    private readonly string jwtKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public LumenApiFactory(params IInterceptor[] interceptors)
    {
        ArgumentNullException.ThrowIfNull(interceptors);
        if (interceptors.Any(x => x is null))
        {
            throw new ArgumentException("Los interceptores no pueden contener elementos nulos.", nameof(interceptors));
        }
        this.interceptors = interceptors.ToArray();
    }

    public Action<IServiceCollection>? ConfigureTestServices { get; init; }

    private readonly string databasePath = Path.Combine(
        Path.GetTempPath(),
        $"lumen-integration-{Guid.NewGuid():N}.db");

    internal string DatabasePath => databasePath;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?> { ["Jwt:Key"] = jwtKey }));
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
            ConfigureTestServices?.Invoke(services);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        foreach (var path in new[] { databasePath, databasePath + "-wal", databasePath + "-shm", databasePath + "-journal" })
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
