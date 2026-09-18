using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Lumen.BusinessLogic.Abstractions;
using Lumen.DataAccess.Entities;
using Microsoft.IdentityModel.Tokens;

namespace Lumen.API.Security;

public sealed class JwtTokenService(IConfiguration configuration) : ITokenService
{
    public TokenData Generate(User user)
    {
        var expires = DateTime.UtcNow.AddMinutes(configuration.GetValue("Jwt:ExpirationMinutes", 120));
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            configuration["Jwt:Key"] ?? throw new InvalidOperationException("Falta configurar Jwt:Key.")));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: credentials);
        return new TokenData(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
