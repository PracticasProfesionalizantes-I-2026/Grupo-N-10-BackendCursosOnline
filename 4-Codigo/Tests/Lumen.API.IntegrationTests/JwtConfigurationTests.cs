using System.Data.Common;
using System.Security.Cryptography;
using Lumen.API.Security;
using Lumen.DataAccess;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lumen.API.IntegrationTests;

public sealed class JwtConfigurationTests
{
    public static TheoryData<string, string> InvalidConfigurations => new()
    {
        { "Development", "missing" }, { "Development", "empty" },
        { "Development", "whitespace" }, { "Development", "short" },
        { "Testing", "missing" }, { "Testing", "empty" },
        { "Testing", "whitespace" }, { "Testing", "short" },
        { "Production", "missing" }, { "Production", "empty" },
        { "Production", "whitespace" }, { "Production", "short" }
    };

    [Theory]
    [MemberData(nameof(InvalidConfigurations))]
    public void InvalidKey_RejectsStartupBeforeOpeningOrCreatingDatabase(string environment, string scenario)
    {
        var probe = new DatabaseOpeningProbe();
        using var root = new LumenApiFactory(probe);
        using var factory = root.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Jwt:Key"] = scenario switch
                {
                    "missing" => null,
                    "empty" => string.Empty,
                    "whitespace" => new string(' ', 40),
                    _ => Convert.ToBase64String(RandomNumberGenerator.GetBytes(8))
                } }));
        });

        var error = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());

        Assert.Contains(error.Failures, failure => failure.Contains("Jwt:Key", StringComparison.Ordinal));
        Assert.Equal(0, probe.OpenAttempts);
        Assert.False(File.Exists(root.DatabasePath));
        Assert.False(File.Exists(root.DatabasePath + "-wal"));
        Assert.False(File.Exists(root.DatabasePath + "-shm"));
    }

    [Theory]
    [InlineData(15, false)]
    [InlineData(16, true)]
    public void KeyLength_IsMeasuredInUtf8Bytes(int characterCount, bool expectedSuccess)
    {
        var options = ValidOptions();
        options.Key = new string('\u00e9', characterCount);

        Assert.Equal(expectedSuccess, new JwtOptionsValidator().Validate(null, options).Succeeded);
    }

    [Theory]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("expiration")]
    public void InvalidMetadata_IsRejectedWithoutDisclosingSigningKey(string scenario)
    {
        var options = ValidOptions();
        if (scenario == "issuer") options.Issuer = string.Empty;
        if (scenario == "audience") options.Audience = string.Empty;
        if (scenario == "expiration") options.ExpirationMinutes = 0;

        var result = new JwtOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
        Assert.False(string.Join(" ", result.Failures).Contains(options.Key!, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ValidFactoryKey_StartsAndInitializesOnlyTemporaryDatabase()
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        Assert.Equal(System.Net.HttpStatusCode.OK, (await client.GetAsync("/openapi/v1.json")).StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LumenDbContext>();

        Assert.Equal(Path.GetFullPath(factory.DatabasePath),
            Path.GetFullPath(context.Database.GetDbConnection().DataSource));
        Assert.StartsWith(Path.GetTempPath(), factory.DatabasePath, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(factory.DatabasePath));
        Assert.Equal(3, await context.Users.CountAsync());
    }

    private static JwtOptions ValidOptions() => new()
    {
        Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
        Issuer = "Lumen.API", Audience = "Lumen.Client", ExpirationMinutes = 120
    };

    private sealed class DatabaseOpeningProbe : DbConnectionInterceptor
    {
        private int attempts;
        public int OpenAttempts => Volatile.Read(ref attempts);

        public override InterceptionResult ConnectionOpening(DbConnection connection,
            ConnectionEventData eventData, InterceptionResult result)
        {
            Interlocked.Increment(ref attempts);
            return result;
        }

        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,
            ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref attempts);
            return ValueTask.FromResult(result);
        }
    }
}
