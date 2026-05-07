using CampusConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusConnect.Infrastructure.Migrations;

[DbContext(typeof(CampusConnectDbContext))]
[Migration("20260504000100_AddUserProfileColumns")]
public partial class AddUserProfileColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Location",
            table: "Users",
            type: "TEXT",
            maxLength: 120,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "PhoneNumber",
            table: "Users",
            type: "TEXT",
            maxLength: 40,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "ProfileNote",
            table: "Users",
            type: "TEXT",
            maxLength: 280,
            nullable: false,
            defaultValue: "");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Location", table: "Users");
        migrationBuilder.DropColumn(name: "PhoneNumber", table: "Users");
        migrationBuilder.DropColumn(name: "ProfileNote", table: "Users");
    }
}