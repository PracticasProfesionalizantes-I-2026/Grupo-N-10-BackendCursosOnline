using System.Text;
using System.Text.Json.Serialization;
using Lumen.API.OpenApi;
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
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Resolve after test-host overrides, but before any migration or database initialization.
_ = app.Services.GetRequiredService<IOptions<JwtOptions>>().Value;

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<DbInitializer>().InitializeAsync(app.Environment.EnvironmentName);
}

app.MapOpenApi();
app.MapScalarApiReference();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program;
