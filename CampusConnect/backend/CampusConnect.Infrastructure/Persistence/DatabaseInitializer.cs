using CampusConnect.Application.Common.Security;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CampusConnect.Infrastructure.Persistence;

public sealed class DatabaseInitializer(CampusConnectDbContext dbContext, IOptions<AdminOptions> adminOptions)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        await EnsureCourseTableAsync(cancellationToken);

        var options = adminOptions.Value;
        var courseCode = options.Course.Trim().ToUpperInvariant();
        await EnsureAdminCourseAsync(courseCode, options, cancellationToken);

        var email = options.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(options.Password))
            return;

        var admin = await dbContext.Users.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
        if (admin is not null)
        {
            if (admin.Role != UserRole.Admin)
            {
                admin.Role = UserRole.Admin;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        dbContext.Users.Add(new User
        {
            Email = email,
            PasswordHash = PasswordHasher.Hash(options.Password),
            DisplayName = options.DisplayName,
            StudyProgram = options.StudyProgram,
            Semester = Math.Max(1, options.Semester),
            Course = string.IsNullOrWhiteSpace(courseCode) ? options.Course : courseCode,
            Role = UserRole.Admin
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCourseTableAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "Courses" (
                "Code" TEXT NOT NULL CONSTRAINT "PK_Courses" PRIMARY KEY,
                "StudyProgram" TEXT NOT NULL,
                "Semester" INTEGER NOT NULL,
                "IsActive" INTEGER NOT NULL,
                "CreatedAt" TEXT NOT NULL
            );
            """, cancellationToken);
    }

    private async Task EnsureAdminCourseAsync(string courseCode, AdminOptions options, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(courseCode))
            return;

        var studyProgram = string.IsNullOrWhiteSpace(options.StudyProgram)
            ? "Administration"
            : options.StudyProgram.Trim();
        var semester = Math.Clamp(options.Semester, 1, 6);

        var existing = await dbContext.Courses.FirstOrDefaultAsync(course => course.Code == courseCode, cancellationToken);
        if (existing is null)
        {
            dbContext.Courses.Add(new Course
            {
                Code = courseCode,
                StudyProgram = studyProgram,
                Semester = semester,
                IsActive = true
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var changed = false;
        if (!existing.IsActive)
        {
            existing.IsActive = true;
            changed = true;
        }

        if (existing.StudyProgram != studyProgram)
        {
            existing.StudyProgram = studyProgram;
            changed = true;
        }

        if (existing.Semester != semester)
        {
            existing.Semester = semester;
            changed = true;
        }

        if (changed)
            await dbContext.SaveChangesAsync(cancellationToken);
    }
}
