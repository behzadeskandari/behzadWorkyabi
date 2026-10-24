using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IranJob.Modules.Candidates.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateResumeDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CandidateResumes",
                schema: "candidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfessionalSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CompletenessScore = table.Column<int>(type: "int", nullable: false),
                    CvFileName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CvFileContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CvFileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    CvUploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateResumes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResumeCertifications",
                schema: "candidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResumeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IssuingOrganization = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CertificateUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResumeCertifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResumeCertifications_CandidateResumes_ResumeId",
                        column: x => x.ResumeId,
                        principalSchema: "candidates",
                        principalTable: "CandidateResumes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResumeEducations",
                schema: "candidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResumeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Institution = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Degree = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FieldOfStudy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Grade = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResumeEducations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResumeEducations_CandidateResumes_ResumeId",
                        column: x => x.ResumeId,
                        principalSchema: "candidates",
                        principalTable: "CandidateResumes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResumeLanguages",
                schema: "candidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResumeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProficiencyLevel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResumeLanguages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResumeLanguages_CandidateResumes_ResumeId",
                        column: x => x.ResumeId,
                        principalSchema: "candidates",
                        principalTable: "CandidateResumes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResumePortfolioLinks",
                schema: "candidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResumeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResumePortfolioLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResumePortfolioLinks_CandidateResumes_ResumeId",
                        column: x => x.ResumeId,
                        principalSchema: "candidates",
                        principalTable: "CandidateResumes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResumeProjects",
                schema: "candidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResumeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResumeProjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResumeProjects_CandidateResumes_ResumeId",
                        column: x => x.ResumeId,
                        principalSchema: "candidates",
                        principalTable: "CandidateResumes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResumeSkills",
                schema: "candidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResumeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProficiencyLevel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    YearsOfExperience = table.Column<int>(type: "int", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResumeSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResumeSkills_CandidateResumes_ResumeId",
                        column: x => x.ResumeId,
                        principalSchema: "candidates",
                        principalTable: "CandidateResumes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResumeWorkExperiences",
                schema: "candidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResumeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Company = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    JobTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResumeWorkExperiences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResumeWorkExperiences_CandidateResumes_ResumeId",
                        column: x => x.ResumeId,
                        principalSchema: "candidates",
                        principalTable: "CandidateResumes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateResumes_UserId",
                schema: "candidates",
                table: "CandidateResumes",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResumeCertifications_ResumeId_SortOrder",
                schema: "candidates",
                table: "ResumeCertifications",
                columns: new[] { "ResumeId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ResumeEducations_ResumeId_SortOrder",
                schema: "candidates",
                table: "ResumeEducations",
                columns: new[] { "ResumeId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ResumeLanguages_ResumeId_SortOrder",
                schema: "candidates",
                table: "ResumeLanguages",
                columns: new[] { "ResumeId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ResumePortfolioLinks_ResumeId_SortOrder",
                schema: "candidates",
                table: "ResumePortfolioLinks",
                columns: new[] { "ResumeId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ResumeProjects_ResumeId_SortOrder",
                schema: "candidates",
                table: "ResumeProjects",
                columns: new[] { "ResumeId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ResumeSkills_ResumeId_SkillId",
                schema: "candidates",
                table: "ResumeSkills",
                columns: new[] { "ResumeId", "SkillId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResumeSkills_ResumeId_SortOrder",
                schema: "candidates",
                table: "ResumeSkills",
                columns: new[] { "ResumeId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ResumeWorkExperiences_ResumeId_SortOrder",
                schema: "candidates",
                table: "ResumeWorkExperiences",
                columns: new[] { "ResumeId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResumeCertifications",
                schema: "candidates");

            migrationBuilder.DropTable(
                name: "ResumeEducations",
                schema: "candidates");

            migrationBuilder.DropTable(
                name: "ResumeLanguages",
                schema: "candidates");

            migrationBuilder.DropTable(
                name: "ResumePortfolioLinks",
                schema: "candidates");

            migrationBuilder.DropTable(
                name: "ResumeProjects",
                schema: "candidates");

            migrationBuilder.DropTable(
                name: "ResumeSkills",
                schema: "candidates");

            migrationBuilder.DropTable(
                name: "ResumeWorkExperiences",
                schema: "candidates");

            migrationBuilder.DropTable(
                name: "CandidateResumes",
                schema: "candidates");
        }
    }
}
