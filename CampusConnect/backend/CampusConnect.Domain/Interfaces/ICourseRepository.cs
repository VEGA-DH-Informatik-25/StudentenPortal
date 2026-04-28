using CampusConnect.Domain.Entities;

namespace CampusConnect.Domain.Interfaces;

public interface ICourseRepository
{
    Task<IReadOnlyList<Course>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Course?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task AddAsync(Course course, CancellationToken cancellationToken = default);
}
