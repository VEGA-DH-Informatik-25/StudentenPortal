using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Interfaces;
using CampusConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Infrastructure.Repositories;

public sealed class EntityExamRepository(CampusConnectDbContext dbContext) : IExamRepository
{
    public async Task<IReadOnlyList<ExamEntry>> GetByUserAsync(Guid userId) =>
        await dbContext.ExamEntries
            .AsNoTracking()
            .Where(entry => entry.UserId == userId)
            .Select(entry => Clone(entry))
            .ToListAsync();

    public async Task AddAsync(ExamEntry entry)
    {
        var existing = await dbContext.ExamEntries.FirstOrDefaultAsync(item => item.Id == entry.Id);
        if (existing is null)
        {
            dbContext.ExamEntries.Add(Clone(entry));
        }
        else
        {
            existing.UserId = entry.UserId;
            existing.ModuleName = entry.ModuleName;
            existing.ExamDate = entry.ExamDate;
            existing.Location = entry.Location;
            existing.Notes = entry.Notes;
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var entry = await dbContext.ExamEntries.FirstOrDefaultAsync(item => item.Id == id && item.UserId == userId);
        if (entry is null)
            return;

        dbContext.ExamEntries.Remove(entry);
        await dbContext.SaveChangesAsync();
    }

    private static ExamEntry Clone(ExamEntry entry) => new()
    {
        Id = entry.Id,
        UserId = entry.UserId,
        ModuleName = entry.ModuleName,
        ExamDate = entry.ExamDate,
        Location = entry.Location,
        Notes = entry.Notes,
        CreatedAt = entry.CreatedAt
    };
}