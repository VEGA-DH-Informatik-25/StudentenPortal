using CampusConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusConnect.Infrastructure.Migrations;

[DbContext(typeof(CampusConnectDbContext))]
[Migration("20260628090000_AddFeedPostAttachmentsAndTranslations")]
public partial class AddFeedPostAttachmentsAndTranslations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Attachments",
            table: "FeedPosts",
            type: "TEXT",
            nullable: false,
            defaultValue: "[]");

        migrationBuilder.AddColumn<string>(
            name: "Translations",
            table: "FeedPosts",
            type: "TEXT",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Attachments",
            table: "FeedPosts");

        migrationBuilder.DropColumn(
            name: "Translations",
            table: "FeedPosts");
    }
}
