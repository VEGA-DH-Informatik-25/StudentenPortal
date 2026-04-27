using CampusConnect.Domain.Entities;

namespace CampusConnect.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
}
