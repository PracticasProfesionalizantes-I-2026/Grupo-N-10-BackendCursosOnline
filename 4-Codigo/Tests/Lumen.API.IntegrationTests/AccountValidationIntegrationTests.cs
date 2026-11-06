using System.Net;
using System.Net.Http.Json;
using Lumen.DataAccess;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Lumen.Shared.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Lumen.API.IntegrationTests.ConcurrencyTestSupport;

namespace Lumen.API.IntegrationTests;

public sealed class AccountValidationIntegrationTests
{
    [Theory]
    [InlineData("role", null)]
    [InlineData("role", "Administrador")]
    [InlineData("email", "@")]
    [InlineData("email", "Name <valid@example.test>")]
    [InlineData("firstName", " ")]
    [InlineData("firstName", "over-limit")]
    [InlineData("dni", "AB\n123")]
    [InlineData("phone", "+54\u000011")]
    public async Task PublicRegistrationRejectsInvalidInputWithoutCreatingUser(string field, string? value)
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        var payload = Registration();
        if (field == "role" && value is null)
            payload.Remove(field);
        else
            payload[field] = value == "over-limit" ? new string('A', UserFieldLimits.Name + 1) : value;
        var before = await CountUsersAsync(factory);
        using var response = await client.PostAsJsonAsync("/api/auth/register", payload, Json);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, await CountUsersAsync(factory));
    }

    [Fact]
    public async Task RegistrationAcceptsBoundaryAndDoesNotAdvertiseUnusableLocation()
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        var payload = Registration();
        payload["firstName"] = $" {new string('A', UserFieldLimits.Name)} ";
        payload["dni"] = "AB-123";
        payload["phone"] = "+54 (11) 1234-5678";
        using var response = await client.PostAsJsonAsync("/api/auth/register", payload, Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Null(response.Headers.Location);
        var result = await response.Content.ReadFromJsonAsync<UserResponseDTO>(Json);
        Assert.Equal(UserFieldLimits.Name, result!.FirstName.Length);
        Assert.Equal(UserRole.Profesor, result.Role);
        Assert.Equal("AB-123", result.Dni);
        Assert.Equal("+54 (11) 1234-5678", result.Phone);
    }

    [Fact]
    public async Task InternalRegistrationUsesSameFieldLimits()
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, DemoAccounts.AdministratorEmail);
        var payload = Registration();
        payload["postalCode"] = new string('1', UserFieldLimits.PostalCode + 1);
        var before = await CountUsersAsync(factory);
        using var response = await client.PostAsJsonAsync("/api/users/administrators", payload, Json);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, await CountUsersAsync(factory));
    }

    [Fact]
    public async Task LoginRevealsInactiveStateOnlyAfterCorrectPassword()
    {
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
            await db.Users.Where(x => x.Email == DemoAccounts.StudentEmail)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsActive, false));
        }
        using var invalid = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDTO(DemoAccounts.StudentEmail, "invalid-test-password"), Json);
        using var valid = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDTO(DemoAccounts.StudentEmail, DbInitializer.DemoPassword), Json);
        Assert.Equal(HttpStatusCode.Unauthorized, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, valid.StatusCode);
    }

    [Fact]
    public async Task LoginPreservesAccessForAnAddressAcceptedBeforeRegistrationValidation()
    {
        const string legacyEmail = "legacy@";
        using var factory = new LumenApiFactory();
        using var client = factory.CreateClient();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
            await db.Users.Where(x => x.Email == DemoAccounts.StudentEmail)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Email, legacyEmail)
                    .SetProperty(x => x.NormalizedEmail, legacyEmail.ToUpperInvariant()));
        }
        using var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDTO(legacyEmail, DbInitializer.DemoPassword), Json);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static Dictionary<string, object?> Registration() => new()
    {
        ["email"] = "registration@example.test", ["password"] = "synthetic-test-password",
        ["role"] = "Profesor", ["firstName"] = "Nombre", ["lastName"] = "Apellido",
        ["dni"] = "12345678", ["phone"] = "123456", ["address"] = "Dirección", ["postalCode"] = "1000"
    };

    private static async Task<int> CountUsersAsync(LumenApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<LumenDbContext>().Users.CountAsync();
    }
}
