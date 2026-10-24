using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IranJob.Modules.JobPostings.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedJobsAndJobAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobAlerts",
                schema: "jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkArrangement = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobAlerts_CandidateProfiles_CandidateProfileId",
                        column: x => x.CandidateProfileId,
                        principalSchema: "candidates",
                        principalTable: "CandidateProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobAlerts_Cities_CityId",
                        column: x => x.CityId,
                        principalSchema: "reference_data",
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobAlerts_JobCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "reference_data",
                        principalTable: "JobCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SavedJobs",
                schema: "jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobPostingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SavedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedJobs_CandidateProfiles_CandidateProfileId",
                        column: x => x.CandidateProfileId,
                        principalSchema: "candidates",
                        principalTable: "CandidateProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavedJobs_JobPostings_JobPostingId",
                        column: x => x.JobPostingId,
                        principalSchema: "jobs",
                        principalTable: "JobPostings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobAlertSkills",
                schema: "jobs",
                columns: table => new
                {
                    JobAlertId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobAlertSkills", x => new { x.JobAlertId, x.SkillId });
                    table.ForeignKey(
                        name: "FK_JobAlertSkills_JobAlerts_JobAlertId",
                        column: x => x.JobAlertId,
                        principalSchema: "jobs",
                        principalTable: "JobAlerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobAlertSkills_Skills_SkillId",
                        column: x => x.SkillId,
                        principalSchema: "reference_data",
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobAlerts_CandidateProfileId_IsEnabled",
                schema: "jobs",
                table: "JobAlerts",
                columns: new[] { "CandidateProfileId", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_JobAlerts_CategoryId",
                schema: "jobs",
                table: "JobAlerts",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_JobAlerts_CityId",
                schema: "jobs",
                table: "JobAlerts",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_JobAlertSkills_SkillId",
                schema: "jobs",
                table: "JobAlertSkills",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedJobs_CandidateProfileId_JobPostingId",
                schema: "jobs",
                table: "SavedJobs",
                columns: new[] { "CandidateProfileId", "JobPostingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedJobs_CandidateProfileId_SavedAt",
                schema: "jobs",
                table: "SavedJobs",
                columns: new[] { "CandidateProfileId", "SavedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedJobs_JobPostingId",
                schema: "jobs",
                table: "SavedJobs",
                column: "JobPostingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobAlertSkills",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "SavedJobs",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "JobAlerts",
                schema: "jobs");
        }
    }
}
