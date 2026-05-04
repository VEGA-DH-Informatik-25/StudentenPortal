using CampusConnect.Application.Common.Security;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CampusConnect.Infrastructure.Persistence;

public sealed class DatabaseInitializer(CampusConnectDbContext dbContext, IOptions<AdminOptions> adminOptions)
{
    private const string EfProductVersion = "10.0.7";
    private const string InitialUsersMigrationId = "20260504000000_InitialUsers";
    private const string AddUserProfileColumnsMigrationId = "20260504000100_AddUserProfileColumns";
    private const string AddCoursesMigrationId = "20260504000200_AddCourses";
    private const string AddPersistentFeatureTablesMigrationId = "20260504000300_AddPersistentFeatureTables";

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await EnsureMigrationBaselineAsync(cancellationToken);
        await dbContext.Database.MigrateAsync(cancellationToken);

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

    private async Task EnsureMigrationBaselineAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            var hasUsers = await TableExistsAsync("Users", cancellationToken);
            var hasCourses = await TableExistsAsync("Courses", cancellationToken);
            if (!hasUsers && !hasCourses)
                return;

            await EnsureMigrationHistoryTableAsync(cancellationToken);
            var appliedMigrations = await GetAppliedMigrationsAsync(cancellationToken);

            if (hasUsers && !appliedMigrations.Contains(InitialUsersMigrationId))
                await InsertMigrationHistoryAsync(InitialUsersMigrationId, cancellationToken);

            if (hasUsers && !appliedMigrations.Contains(AddUserProfileColumnsMigrationId))
            {
                var userColumns = await GetTableColumnsAsync("Users", cancellationToken);
                if (userColumns.IsSupersetOf(["PhoneNumber", "Location", "ProfileNote"]))
                    await InsertMigrationHistoryAsync(AddUserProfileColumnsMigrationId, cancellationToken);
            }

            if (hasCourses && !appliedMigrations.Contains(AddCoursesMigrationId))
                await InsertMigrationHistoryAsync(AddCoursesMigrationId, cancellationToken);

            if (!appliedMigrations.Contains(AddPersistentFeatureTablesMigrationId) &&
                await TableExistsAsync("CampusGroups", cancellationToken) &&
                await TableExistsAsync("FeedPosts", cancellationToken) &&
                await TableExistsAsync("Grades", cancellationToken) &&
                await TableExistsAsync("ExamEntries", cancellationToken))
            {
                await InsertMigrationHistoryAsync(AddPersistentFeatureTablesMigrationId, cancellationToken);
            }
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }

    private async Task EnsureMigrationHistoryTableAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL
            );
            """, cancellationToken);
    }

    private async Task<HashSet<string>> GetAppliedMigrationsAsync(CancellationToken cancellationToken)
    {
        var migrations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var connection = dbContext.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\";";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            migrations.Add(reader.GetString(0));

        return migrations;
    }

    private async Task<HashSet<string>> GetTableColumnsAsync(string tableName, CancellationToken cancellationToken)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var connection = dbContext.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{tableName}\");";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            columns.Add(reader.GetString(1));

        return columns;
    }

    private async Task<bool> TableExistsAsync(string tableName, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name;";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "$name";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is long count && count > 0;
    }

    private async Task InsertMigrationHistoryAsync(string migrationId, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT OR IGNORE INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ($migrationId, $productVersion);";

        var migrationParameter = command.CreateParameter();
        migrationParameter.ParameterName = "$migrationId";
        migrationParameter.Value = migrationId;
        command.Parameters.Add(migrationParameter);

        var versionParameter = command.CreateParameter();
        versionParameter.ParameterName = "$productVersion";
        versionParameter.Value = EfProductVersion;
        command.Parameters.Add(versionParameter);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
