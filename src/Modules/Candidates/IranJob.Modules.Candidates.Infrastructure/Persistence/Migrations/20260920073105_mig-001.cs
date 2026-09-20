using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IranJob.Modules.Candidates.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class mig001 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                schema: "candidates",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "Phone",
                schema: "candidates",
                table: "CandidateProfiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                schema: "candidates",
                table: "CandidateProfiles",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                schema: "candidates",
                table: "CandidateProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }
    }
}
