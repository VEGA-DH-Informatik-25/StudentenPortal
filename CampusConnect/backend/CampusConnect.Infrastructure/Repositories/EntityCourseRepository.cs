using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Interfaces;
using CampusConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Infrastructure.Repositories;

public class EntityCourseRepository(CampusConnectDbContext dbContext) : ICourseRepository
{
    public async Task<IReadOnlyList<Course>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Courses
            .AsNoTracking()
            .OrderBy(course => course.Code)
            .ToListAsync(cancellationToken);

    public async Task<Course?> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = NormalizeCode(code);
        return await dbContext.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(course => course.Code == normalizedCode, cancellationToken);
    }

    public async Task AddAsync(Course course, CancellationToken cancellationToken = default)
    {
        course.Code = NormalizeCode(course.Code);
        dbContext.Courses.Add(course);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
}
