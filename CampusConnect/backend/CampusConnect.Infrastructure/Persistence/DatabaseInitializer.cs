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

        var options = adminOptions.Value;
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
            Course = options.Course,
            Role = UserRole.Admin
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}