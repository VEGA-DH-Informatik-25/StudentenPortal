using CampusConnect.Domain.Entities;

namespace CampusConnect.Domain.Interfaces;

public interface IExamRepository
{
    Task<IReadOnlyList<ExamEntry>> GetByUserAsync(Guid userId);
    Task AddAsync(ExamEntry entry);
    Task DeleteAsync(Guid id, Guid userId);
}
