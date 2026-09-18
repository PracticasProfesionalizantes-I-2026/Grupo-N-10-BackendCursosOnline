using Lumen.DataAccess.Entities;

namespace Lumen.BusinessLogic.Abstractions;

public sealed record TokenData(string AccessToken, DateTime ExpiresAtUtc);

public interface ITokenService
{
    TokenData Generate(User user);
}

