using CampusConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusConnect.Infrastructure.Migrations;

[DbContext(typeof(CampusConnectDbContext))]
[Migration("20260612095500_AddFeedPostModeration")]
public partial class AddFeedPostModeration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "AllowComments",
            table: "FeedPosts",
            type: "INTEGER",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<string>(
            name: "Status",
            table: "FeedPosts",
            type: "TEXT",
            maxLength: 16,
            nullable: false,
            defaultValue: "Published");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AllowComments",
            table: "FeedPosts");

        migrationBuilder.DropColumn(
            name: "Status",
            table: "FeedPosts");
    }
}
