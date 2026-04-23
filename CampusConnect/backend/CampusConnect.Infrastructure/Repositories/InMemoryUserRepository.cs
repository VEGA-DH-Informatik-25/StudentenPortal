using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Interfaces;
using System.Collections.Concurrent;

namespace CampusConnect.Infrastructure.Repositories;

public class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<Guid, User> _store = new();

    public Task<User?> FindByEmailAsync(string email)
    {
        var user = _store.Values.FirstOrDefault(u => u.Email == email.ToLowerInvariant());
        return Task.FromResult(user);
    }

    public Task<User?> FindByIdAsync(Guid id)
    {
        _store.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task AddAsync(User user)
    {
        _store[user.Id] = user;
        return Task.CompletedTask;
    }
}
