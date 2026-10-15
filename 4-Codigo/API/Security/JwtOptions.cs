using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Lumen.API.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string? Key { get; set; }
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
    public int ExpirationMinutes { get; set; } = 120;
}

public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    // Fingerprint of the revoked repository key, never a usable signing key.
    private static readonly byte[] RevokedKeyHash = Convert.FromHexString(
        "7B72791C96C840145F11152A8177F86AFD581B53D7E79CFA80DF8AA2797F7653");

    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(options.Key))
        {
            failures.Add("Falta configurar Jwt:Key mediante User Secrets o la variable Jwt__Key.");
        }
        else
        {
            var keyBytes = Encoding.UTF8.GetBytes(options.Key);
            if (keyBytes.Length < 32)
            {
                failures.Add("Jwt:Key debe contener al menos 32 bytes UTF-8.");
            }
            if (CryptographicOperations.FixedTimeEquals(SHA256.HashData(keyBytes), RevokedKeyHash))
            {
                failures.Add("Jwt:Key utiliza una clave revocada. Configurá una nueva clave externa.");
            }
        }
        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            failures.Add("Falta configurar Jwt:Issuer.");
        }
        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add("Falta configurar Jwt:Audience.");
        }
        if (options.ExpirationMinutes <= 0)
        {
            failures.Add("Jwt:ExpirationMinutes debe ser mayor que cero.");
        }
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
