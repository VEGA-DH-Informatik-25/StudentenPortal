using CampusConnect.Domain.Entities;

namespace CampusConnect.Domain.Interfaces;

public interface IGradeRepository
{
    Task<IReadOnlyList<Grade>> GetByUserAsync(Guid userId);
    Task AddAsync(Grade grade);
    Task DeleteAsync(Guid id, Guid userId);
}
