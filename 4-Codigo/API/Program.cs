using System.Text;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Reflection;
using Lumen.API.OpenApi;
using Lumen.API.Errors;
using Lumen.API.Security;
using Lumen.BusinessLogic;
using Lumen.BusinessLogic.Abstractions;
using Lumen.DataAccess;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging.EventLog;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

if (OperatingSystem.IsWindows())
{
    // Keep Console and the other providers; Windows EventLog needs OS-level write access.
    foreach (var provider in builder.Services.Where(descriptor =>
        descriptor.ServiceType == typeof(ILoggerProvider) &&
        descriptor.ImplementationType == typeof(EventLogLoggerProvider)).ToArray())
    {
        builder.Services.Remove(provider);
    }
}

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecurityDocumentTransformer>();
    options.AddOperationTransformer<EndpointContractOperationTransformer>();
    options.AddSchemaTransformer<EnumContractSchemaTransformer>();
    options.AddDocumentTransformer<EnumContractSchemaTransformer>();
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddBusinessLogic();

var connectionString = builder.Configuration.GetConnectionString("Lumen")
    ?? "Data Source=lumen.db";
builder.Services.AddDataAccess(connectionString);

builder.Services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateOnStart();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key!)),
            ClockSkew = TimeSpan.FromSeconds(15)
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var host = context.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();
                var email = context.Principal?.FindFirstValue(ClaimTypes.Email);
                if (!DemoAccounts.IsAllowedIn(host.EnvironmentName) && DemoAccounts.IsReserved(email))
                {
                    context.Fail("La identidad de demostración no está habilitada en este entorno.");
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(LoginRateLimit.Configure);

var app = builder.Build();

// Resolve after test-host overrides, but before any migration or database initialization.
try
{
    _ = app.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
}
catch (OptionsValidationException)
{
    Console.Error.WriteLine("Configuración JWT ausente o inválida. La API no puede iniciar.");
    Console.Error.WriteLine("Verificá Jwt:Key (clave propia de al menos 32 bytes UTF-8), Jwt:Issuer y Jwt:Audience desde 4-Codigo:");
    Console.Error.WriteLine("  Generá la clave y enviá Jwt:Key por entrada estándar a dotnet user-secrets set --project API/Lumen.API.csproj.");
    Console.Error.WriteLine("También podés establecer la variable de entorno Jwt__Key. Consultá 4-Codigo/README.md para ver el comando completo sin mostrar la clave.");
    // In-process hosts retain the exception for their own lifecycle and diagnostics.
    if (Assembly.GetEntryAssembly() != typeof(Program).Assembly) throw;
    Environment.ExitCode = 1;
    return;
}

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<DbInitializer>().InitializeAsync(app.Environment.EnvironmentName);
}

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseRouting();
app.UseRateLimiter();
app.MapOpenApi();
app.MapScalarApiReference();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program;
