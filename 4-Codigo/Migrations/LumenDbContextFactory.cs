using Lumen.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lumen.Migrations;

public sealed class LumenDbContextFactory : IDesignTimeDbContextFactory<LumenDbContext>
{
    public LumenDbContext CreateDbContext(string[] args)
    {
        var connectionString = args.FirstOrDefault(x => x.StartsWith("--connection=", StringComparison.OrdinalIgnoreCase))?
            .Split('=', 2)[1] ?? "Data Source=lumen.db";
        var options = new DbContextOptionsBuilder<LumenDbContext>()
            .UseSqlite(connectionString, sqlite => sqlite.MigrationsAssembly(typeof(LumenDbContextFactory).Assembly.FullName))
            .Options;
        return new LumenDbContext(options);
    }
}
