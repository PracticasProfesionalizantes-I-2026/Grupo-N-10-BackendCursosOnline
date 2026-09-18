using Lumen.BusinessLogic.Abstractions;
using Lumen.Shared.Enums;

namespace Lumen.BusinessLogic.Tests;

internal sealed class TestCurrentUser : ICurrentUser
{
    public bool IsAuthenticated { get; init; } = true;
    public Guid UserId { get; init; } = Guid.NewGuid();
    public UserRole Role { get; init; }
}

