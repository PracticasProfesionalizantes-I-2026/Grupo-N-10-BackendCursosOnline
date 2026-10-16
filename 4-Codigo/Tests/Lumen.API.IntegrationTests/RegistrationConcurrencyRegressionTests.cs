using System.Net;
using System.Net.Http.Json;
using Lumen.DataAccess;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Lumen.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using static Lumen.API.IntegrationTests.ConcurrencyTestSupport;

namespace Lumen.API.IntegrationTests;

public sealed class RegistrationConcurrencyRegressionTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("race@lumen.local", "RACE@LUMEN.LOCAL")]
    [InlineData("  race@lumen.local  ", "race@lumen.local")]
    [InlineData("  RaCe@LuMeN.LoCaL  ", "RACE@LUMEN.LOCAL")]
    public async Task CU01_OverlappingEquivalentEmails_ReturnCreatedAndConflictWithSingleAccount(string lateEmail, string winnerEmail)
    {
        var gate = new ReadGate();
        using var factory = CreateFactory(gate);
        using var lateClient = factory.CreateClient();
        using var winnerClient = factory.CreateClient();
        // Ensure initialization finishes before the gate is armed.
        await AuthenticateAsync(winnerClient, "admin@lumen.local");
        const string normalizedEmail = "RACE@LUMEN.LOCAL";
        gate.ArmEmail(normalizedEmail);
        var lateTask = CaptureAsync(() => lateClient.PostAsJsonAsync("/api/auth/register",
            Registration(lateEmail, UserRole.Profesor), Json));
        try
        {
            await gate.Entered.Task.WaitAsync(Deadline);
            Assert.False(gate.ReadEmailExists);
            using var winner = await CaptureAsync(() => winnerClient.PostAsJsonAsync("/api/auth/register",
                Registration(winnerEmail, UserRole.Alumno), Json));
            AssertHttp(winner, HttpStatusCode.Created);
            var created = await winner.Response!.Content.ReadFromJsonAsync<UserResponseDTO>(Json);
            Assert.NotNull(created);
            gate.Release.TrySetResult();
            using var late = await lateTask.WaitAsync(Deadline);
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
            var users = await context.Users.AsNoTracking().Where(x => x.NormalizedEmail == normalizedEmail).ToArrayAsync();
            var error = late.Response?.StatusCode == HttpStatusCode.Conflict
                ? await late.Response.Content.ReadFromJsonAsync<ApiErrorResponseDTO>(Json) : null;
            Report(output, "Winning registration", winner);
            Report(output, "Stale registration", late);
            output.WriteLine($"Persisted equivalent-email accounts: {users.Length}");
            Assert.Multiple(
                () => AssertHttp(late, HttpStatusCode.Conflict),
                () => Assert.Equal(nameof(DuplicateEmailException), error?.Code),
                () => Assert.Equal(2, gate.Contexts.Count),
                () => { var user = Assert.Single(users); Assert.Equal(created.Id, user.Id);
                    Assert.Equal(winnerEmail.Trim(), user.Email); Assert.Equal(normalizedEmail, user.NormalizedEmail);
                    Assert.Equal(UserRole.Alumno, user.Role); Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash)); });
        }
        finally { gate.Release.TrySetResult(); await ObserveAsync(lateTask); }
    }

    private static UserCreateDTO Registration(string email, UserRole role) => new(
        email, DbInitializer.DemoPassword, role, "Cuenta", "Concurrente", "12345678", "123456", "Dirección", "1000");
}
