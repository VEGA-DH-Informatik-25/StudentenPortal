using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Interfaces;
using CampusConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Infrastructure.Repositories;

public sealed class EntityGradeRepository(CampusConnectDbContext dbContext) : IGradeRepository
{
    public async Task<IReadOnlyList<Grade>> GetByUserAsync(Guid userId) =>
        await dbContext.Grades
            .AsNoTracking()
            .Where(grade => grade.UserId == userId)
            .OrderByDescending(grade => grade.CreatedAt)
            .Select(grade => Clone(grade))
            .ToListAsync();

    public async Task AddAsync(Grade grade)
    {
        var existing = await dbContext.Grades.FirstOrDefaultAsync(item => item.Id == grade.Id);
        if (existing is null)
        {
            dbContext.Grades.Add(Clone(grade));
        }
        else
        {
            existing.UserId = grade.UserId;
            existing.ModuleCode = grade.ModuleCode;
            existing.ModuleName = grade.ModuleName;
            existing.Value = grade.Value;
            existing.Ects = grade.Ects;
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var grade = await dbContext.Grades.FirstOrDefaultAsync(item => item.Id == id && item.UserId == userId);
        if (grade is null)
            return;

        dbContext.Grades.Remove(grade);
        await dbContext.SaveChangesAsync();
    }

    private static Grade Clone(Grade grade) => new()
    {
        Id = grade.Id,
        UserId = grade.UserId,
        ModuleCode = grade.ModuleCode,
        ModuleName = grade.ModuleName,
        Value = grade.Value,
        Ects = grade.Ects,
        CreatedAt = grade.CreatedAt
    };
}