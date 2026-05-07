using System;
using CampusConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusConnect.Infrastructure.Migrations;

[DbContext(typeof(CampusConnectDbContext))]
[Migration("20260504000300_AddPersistentFeatureTables")]
public partial class AddPersistentFeatureTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CampusGroups",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false),
                Type = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                Audience = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                CourseCode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                OwnerUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                OwnerLabel = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                IconLabel = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                AccentColor = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                Settings = table.Column<string>(type: "TEXT", nullable: false),
                AssignedUserIds = table.Column<string>(type: "TEXT", nullable: false),
                MemberPermissions = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CampusGroups", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ExamEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                ModuleName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                ExamDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                Location = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ExamEntries", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "FeedPosts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                AuthorId = table.Column<Guid>(type: "TEXT", nullable: false),
                GroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                AuthorName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                Content = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                Comments = table.Column<string>(type: "TEXT", nullable: false),
                Reactions = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FeedPosts", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Grades",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                ModuleCode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                ModuleName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                Value = table.Column<decimal>(type: "TEXT", precision: 3, scale: 1, nullable: false),
                Ects = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Grades", x => x.Id);
            });

        migrationBuilder.CreateIndex(name: "IX_CampusGroups_CourseCode", table: "CampusGroups", column: "CourseCode");
        migrationBuilder.CreateIndex(name: "IX_ExamEntries_UserId", table: "ExamEntries", column: "UserId");
        migrationBuilder.CreateIndex(name: "IX_FeedPosts_CreatedAt", table: "FeedPosts", column: "CreatedAt");
        migrationBuilder.CreateIndex(name: "IX_FeedPosts_GroupId", table: "FeedPosts", column: "GroupId");
        migrationBuilder.CreateIndex(name: "IX_Grades_UserId", table: "Grades", column: "UserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CampusGroups");
        migrationBuilder.DropTable(name: "ExamEntries");
        migrationBuilder.DropTable(name: "FeedPosts");
        migrationBuilder.DropTable(name: "Grades");
    }
}