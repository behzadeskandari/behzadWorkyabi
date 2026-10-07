using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IranJob.Modules.JobPostings.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialJobPostings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "jobs");

            migrationBuilder.CreateTable(
                name: "JobPostings",
                schema: "jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployerProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmploymentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WorkArrangement = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SalaryMinimum = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SalaryMaximum = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SalaryCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    SalaryPeriod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosingAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobPostings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobPostings_Cities_CityId",
                        column: x => x.CityId,
                        principalSchema: "reference_data",
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobPostings_EmployerProfiles_EmployerProfileId",
                        column: x => x.EmployerProfileId,
                        principalSchema: "employers",
                        principalTable: "EmployerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobPostings_JobCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "reference_data",
                        principalTable: "JobCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobPostingSkills",
                schema: "jobs",
                columns: table => new
                {
                    JobPostingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobPostingSkills", x => new { x.JobPostingId, x.SkillId });
                    table.ForeignKey(
                        name: "FK_JobPostingSkills_JobPostings_JobPostingId",
                        column: x => x.JobPostingId,
                        principalSchema: "jobs",
                        principalTable: "JobPostings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobPostingSkills_Skills_SkillId",
                        column: x => x.SkillId,
                        principalSchema: "reference_data",
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobPostings_CategoryId",
                schema: "jobs",
                table: "JobPostings",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_JobPostings_CityId",
                schema: "jobs",
                table: "JobPostings",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_JobPostings_ClosingAt",
                schema: "jobs",
                table: "JobPostings",
                column: "ClosingAt");

            migrationBuilder.CreateIndex(
                name: "IX_JobPostings_EmployerProfileId_Status_CreatedAt",
                schema: "jobs",
                table: "JobPostings",
                columns: new[] { "EmployerProfileId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_JobPostings_Status_PublishedAt",
                schema: "jobs",
                table: "JobPostings",
                columns: new[] { "Status", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_JobPostingSkills_SkillId",
                schema: "jobs",
                table: "JobPostingSkills",
                column: "SkillId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobPostingSkills",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "JobPostings",
                schema: "jobs");
        }
    }
}
