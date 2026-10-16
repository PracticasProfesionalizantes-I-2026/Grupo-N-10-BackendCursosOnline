using Lumen.DataAccess.Entities;
using Lumen.Shared.Exceptions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Lumen.DataAccess.Repositories;

public sealed class UserRepository(LumenDbContext context) : IUserRepository
{
    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        user.Id = Guid.NewGuid();
        await context.Users.AddAsync(user, cancellationToken);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException sqlite &&
            sqlite.SqliteExtendedErrorCode == 2067 &&
            sqlite.Message.Contains("UNIQUE constraint failed: Users.NormalizedEmail'", StringComparison.Ordinal))
        {
            // Translate only this unique index, never other constraints or persistence errors.
            throw new DuplicateEmailException(user.Email);
        }
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

