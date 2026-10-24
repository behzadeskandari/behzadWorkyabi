using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IranJob.Modules.Notifications.Infrastructure.Migrations;

public partial class AddNotificationMessage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Message",
            schema: "notifications",
            table: "Notifications",
            type: "nvarchar(2000)",
            maxLength: 2000,
            nullable: false,
            defaultValue: "");

        migrationBuilder.Sql(
            "UPDATE [notifications].[Notifications] SET [Message] = [Title] WHERE [Message] = N'';");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Message",
            schema: "notifications",
            table: "Notifications");
    }
}
