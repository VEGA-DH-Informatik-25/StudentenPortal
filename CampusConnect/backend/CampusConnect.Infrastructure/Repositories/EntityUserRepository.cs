using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Interfaces;
using CampusConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Infrastructure.Repositories;

public sealed class EntityUserRepository(CampusConnectDbContext dbContext) : IUserRepository
{
    public async Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.DisplayName)
            .ThenBy(user => user.Email)
            .ToListAsync(cancellationToken);

    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await dbContext.Users.FirstOrDefaultAsync(user => user.Email == email.ToLowerInvariant(), cancellationToken);

    public async Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);
        if (user is null)
            return;

        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}