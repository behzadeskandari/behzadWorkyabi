using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IranJob.Modules.JobPostings.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddModerationTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ModeratedAt",
                schema: "jobs",
                table: "JobPostings",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModeratedByUserId",
                schema: "jobs",
                table: "JobPostings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModerationReason",
                schema: "jobs",
                table: "JobPostings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModeratedAt",
                schema: "jobs",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "ModeratedByUserId",
                schema: "jobs",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "ModerationReason",
                schema: "jobs",
                table: "JobPostings");
        }
    }
}
