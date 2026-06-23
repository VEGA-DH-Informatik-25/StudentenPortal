using CampusConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusConnect.Infrastructure.Migrations;

[DbContext(typeof(CampusConnectDbContext))]
[Migration("20260622170000_AddOnboardingStatus")]
public partial class AddOnboardingStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(name: "MustChangePassword", table: "Users", type: "INTEGER", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>(name: "OnboardingCompleted", table: "Users", type: "INTEGER", nullable: false, defaultValue: true);
        migrationBuilder.AddColumn<DateTime>(name: "OnboardingCompletedAt", table: "Users", type: "TEXT", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "MustChangePassword", table: "Users");
        migrationBuilder.DropColumn(name: "OnboardingCompleted", table: "Users");
        migrationBuilder.DropColumn(name: "OnboardingCompletedAt", table: "Users");
    }
}
