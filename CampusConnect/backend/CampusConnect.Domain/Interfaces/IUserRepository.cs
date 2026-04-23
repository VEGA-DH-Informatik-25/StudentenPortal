using CampusConnect.Domain.Entities;

namespace CampusConnect.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email);
    Task<User?> FindByIdAsync(Guid id);
    Task AddAsync(User user);
}
