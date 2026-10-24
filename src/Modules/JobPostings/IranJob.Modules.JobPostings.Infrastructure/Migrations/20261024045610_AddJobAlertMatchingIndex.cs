using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IranJob.Modules.JobPostings.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobAlertMatchingIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_JobAlerts_IsEnabled_CategoryId_CityId_WorkArrangement",
                schema: "jobs",
                table: "JobAlerts",
                columns: new[] { "IsEnabled", "CategoryId", "CityId", "WorkArrangement" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobAlerts_IsEnabled_CategoryId_CityId_WorkArrangement",
                schema: "jobs",
                table: "JobAlerts");
        }
    }
}
