using Lumen.Shared.Enums;

namespace Lumen.BusinessLogic.Abstractions;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid UserId { get; }
    UserRole Role { get; }
}

