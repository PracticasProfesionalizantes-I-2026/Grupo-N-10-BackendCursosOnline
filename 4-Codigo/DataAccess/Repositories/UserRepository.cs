using Lumen.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lumen.DataAccess.Repositories;

public sealed class UserRepository(LumenDbContext context) : IUserRepository
{
    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        user.Id = Guid.NewGuid();
        await context.Users.AddAsync(user, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        context.Users.AsNoTracking().SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);

    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        context.Users.AsNoTracking().AnyAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Users.AsNoTracking().OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ToListAsync(cancellationToken);
}

