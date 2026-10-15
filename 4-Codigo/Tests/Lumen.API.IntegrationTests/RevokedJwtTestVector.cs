using Microsoft.IdentityModel.Tokens;

namespace Lumen.API.IntegrationTests;

// Public regression vector signed before revoking the old repository key.
// Contains only fictitious claims and a public signature, never the signing key.
internal static class RevokedJwtTestVector
{
    private const string Header = "{\"alg\":\"HS256\",\"typ\":\"JWT\"}";
    private const string Payload = "{\"iss\":\"Lumen.API\",\"aud\":\"Lumen.Client\",\"exp\":4102444800,\"http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier\":\"00000000-0000-0000-0000-000000000001\",\"http://schemas.microsoft.com/ws/2008/06/identity/claims/role\":\"Administrador\"}";
    private const string Signature = "F_VipDL2oETWUEzU0O7eXo3yZPFW6ZDuY73yuuDhC8g";

    internal static string Token => $"{Base64UrlEncoder.Encode(Header)}.{Base64UrlEncoder.Encode(Payload)}.{Signature}";
}
