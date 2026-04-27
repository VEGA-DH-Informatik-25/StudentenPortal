using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Interfaces;
using System.Collections.Concurrent;

namespace CampusConnect.Infrastructure.Repositories;

public class InMemoryGradeRepository : IGradeRepository
{
    private readonly ConcurrentDictionary<Guid, Grade> _store = new();

    public Task<IReadOnlyList<Grade>> GetByUserAsync(Guid userId)
    {
        var grades = _store.Values
            .Where(g => g.UserId == userId)
            .OrderByDescending(g => g.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<Grade>>(grades);
    }

    public Task AddAsync(Grade grade)
    {
        _store[grade.Id] = grade;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, Guid userId)
    {
        if (_store.TryGetValue(id, out var grade) && grade.UserId == userId)
            _store.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
