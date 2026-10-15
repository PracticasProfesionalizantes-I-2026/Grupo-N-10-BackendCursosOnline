using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Lumen.API.Security;
using Lumen.DataAccess;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Lumen.API.IntegrationTests;

public sealed class JwtSecurityIntegrationTests
{
    [Theory]
    [InlineData("admin@lumen.local", HttpStatusCode.OK)]
    [InlineData("alumno@lumen.local", HttpStatusCode.Forbidden)]
    [InlineData("profesor@lumen.local", HttpStatusCode.Forbidden)]
    public async Task LegitimateLogin_PreservesSignatureClaimsAndRoleAuthorization(string email, HttpStatusCode expected)
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        var token = await LoginAsync(client, email);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(SecurityAlgorithms.HmacSha256, parsed.Header.Alg);
        Assert.NotNull(parsed.Claims.SingleOrDefault(x => x.Type == ClaimTypes.NameIdentifier));
        Assert.NotNull(parsed.Claims.SingleOrDefault(x => x.Type == ClaimTypes.Email));
        Assert.NotNull(parsed.Claims.SingleOrDefault(x => x.Type == ClaimTypes.Name));
        Assert.NotNull(parsed.Claims.SingleOrDefault(x => x.Type == ClaimTypes.Role));
        Assert.Equal(expected, await GetUsersAsync(client, token));
    }

    [Fact]
    public async Task RevokedRepositoryKeySignature_IsRejectedDespiteValidLifetimeIssuerAudienceAndAdminRole()
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        var options = factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(RevokedJwtTestVector.Token);
        Assert.Equal(options.Issuer, parsed.Issuer);
        Assert.Contains(options.Audience!, parsed.Audiences);
        Assert.True(parsed.ValidTo > DateTime.UtcNow);
        Assert.Equal("Administrador", parsed.Claims.Single(x => x.Type == ClaimTypes.Role).Value);

        Assert.Equal(HttpStatusCode.Unauthorized, await GetUsersAsync(client, RevokedJwtTestVector.Token));
        Assert.Equal(HttpStatusCode.OK, await GetUsersAsync(client, await LoginAsync(client, "admin@lumen.local")));
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("other-signing-key")]
    [InlineData("tampered-signature")]
    [InlineData("issuer")]
    [InlineData("audience")]
    public async Task InvalidToken_IsRejectedWith401(string scenario)
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        var options = factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
        var key = scenario == "other-signing-key"
            ? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) : options.Key!;
        var token = CreateToken(key,
            scenario == "issuer" ? "another-issuer" : options.Issuer!,
            scenario == "audience" ? "another-audience" : options.Audience!,
            scenario == "expired" ? DateTime.UtcNow.AddMinutes(-5) : DateTime.UtcNow.AddMinutes(5));
        if (scenario == "tampered-signature")
        {
            var segments = token.Split('.');
            segments[2] = (segments[2][0] == 'A' ? "B" : "A") + segments[2][1..];
            token = string.Join('.', segments);
        }

        Assert.Equal(HttpStatusCode.Unauthorized, await GetUsersAsync(client, token));
    }

    [Fact]
    public async Task IndependentFactories_RejectEachOthersTokens()
    {
        using var first = new LumenApiFactory();
        using var second = new LumenApiFactory();
        using var firstClient = first.CreateClient();
        using var secondClient = second.CreateClient();
        var firstToken = await LoginAsync(firstClient, "admin@lumen.local");
        var secondToken = await LoginAsync(secondClient, "admin@lumen.local");

        Assert.Equal(HttpStatusCode.OK, await GetUsersAsync(firstClient, firstToken));
        Assert.Equal(HttpStatusCode.OK, await GetUsersAsync(secondClient, secondToken));
        Assert.Equal(HttpStatusCode.Unauthorized, await GetUsersAsync(firstClient, secondToken));
        Assert.Equal(HttpStatusCode.Unauthorized, await GetUsersAsync(secondClient, firstToken));
    }

    [Fact]
    public async Task DerivedFactory_PreservesRootKeyAndServiceConfiguration()
    {
        var configured = 0;
        using var root = new LumenApiFactory
        {
            ConfigureTestServices = _ => Interlocked.Increment(ref configured)
        };
        using var derived = root.WithWebHostBuilder(builder => builder.ConfigureServices(_ => { }));
        using var rootClient = root.CreateClient();
        using var derivedClient = derived.CreateClient();

        Assert.Equal(HttpStatusCode.OK,
            await GetUsersAsync(derivedClient, await LoginAsync(rootClient, "admin@lumen.local")));
        Assert.Equal(HttpStatusCode.OK,
            await GetUsersAsync(rootClient, await LoginAsync(derivedClient, "admin@lumen.local")));
        Assert.Equal(2, configured);
    }

    [Fact]
    public async Task ConfigurationChangedAfterStartup_DoesNotSplitSigningAndValidationKeys()
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        var originalToken = await LoginAsync(client, "admin@lumen.local");
        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        configuration["Jwt:Key"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        Assert.Equal(HttpStatusCode.OK, await GetUsersAsync(client, originalToken));
        Assert.Equal(HttpStatusCode.OK,
            await GetUsersAsync(client, await LoginAsync(client, "admin@lumen.local")));
    }

    private static async Task<string> LoginAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email, password = DbInitializer.DemoPassword
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }

    private static async Task<HttpStatusCode> GetUsersAsync(HttpClient client, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    private static string CreateToken(string key, string issuer, string audience, DateTime expires) =>
        new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(issuer, audience,
            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
             new Claim(ClaimTypes.Role, "Administrador")],
            expires: expires, signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256)));
}
