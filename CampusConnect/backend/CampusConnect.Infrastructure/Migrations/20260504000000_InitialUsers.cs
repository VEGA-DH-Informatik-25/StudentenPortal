using System;
using CampusConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusConnect.Infrastructure.Migrations;

[DbContext(typeof(CampusConnectDbContext))]
[Migration("20260504000000_InitialUsers")]
public partial class InitialUsers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                PasswordHash = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                DisplayName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                StudyProgram = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                Semester = table.Column<int>(type: "INTEGER", nullable: false),
                Course = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Role = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            table: "Users",
            column: "Email",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Users");
    }
}