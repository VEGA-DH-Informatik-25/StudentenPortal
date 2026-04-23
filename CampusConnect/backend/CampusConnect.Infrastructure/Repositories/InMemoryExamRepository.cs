using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Interfaces;
using System.Collections.Concurrent;

namespace CampusConnect.Infrastructure.Repositories;

public class InMemoryExamRepository : IExamRepository
{
    private readonly ConcurrentDictionary<Guid, ExamEntry> _store = new();

    public Task<IReadOnlyList<ExamEntry>> GetByUserAsync(Guid userId)
    {
        var exams = _store.Values
            .Where(e => e.UserId == userId)
            .ToList();
        return Task.FromResult<IReadOnlyList<ExamEntry>>(exams);
    }

    public Task AddAsync(ExamEntry entry)
    {
        _store[entry.Id] = entry;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, Guid userId)
    {
        if (_store.TryGetValue(id, out var entry) && entry.UserId == userId)
            _store.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
