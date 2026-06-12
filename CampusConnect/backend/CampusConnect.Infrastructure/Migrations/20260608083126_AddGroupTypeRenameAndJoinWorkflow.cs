using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusConnect.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupTypeRenameAndJoinWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Invitations",
                table: "CampusGroups",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "OfficialCategory",
                table: "CampusGroups",
                type: "TEXT",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingJoinRequests",
                table: "CampusGroups",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            // The "Social" group type was renamed to "Campus".
            migrationBuilder.Sql("UPDATE CampusGroups SET Type = 'Campus' WHERE Type = 'Social';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE CampusGroups SET Type = 'Social' WHERE Type = 'Campus';");

            migrationBuilder.DropColumn(
                name: "Invitations",
                table: "CampusGroups");

            migrationBuilder.DropColumn(
                name: "OfficialCategory",
                table: "CampusGroups");

            migrationBuilder.DropColumn(
                name: "PendingJoinRequests",
                table: "CampusGroups");
        }
    }
}
