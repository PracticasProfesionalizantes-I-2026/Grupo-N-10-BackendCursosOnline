using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Lumen.BusinessLogic.Abstractions;
using Lumen.DataAccess.Entities;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;

namespace Lumen.API.Security;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions jwt = options.Value;

    public TokenData Generate(User user)
    {
        var expires = DateTime.UtcNow.AddMinutes(jwt.ExpirationMinutes);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);
        return new TokenData(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
