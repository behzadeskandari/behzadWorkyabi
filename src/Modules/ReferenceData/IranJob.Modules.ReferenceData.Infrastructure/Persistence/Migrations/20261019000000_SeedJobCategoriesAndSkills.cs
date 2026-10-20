using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IranJob.Modules.ReferenceData.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedJobCategoriesAndSkills : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM reference_data.JobCategories WHERE Id = 'A1000000-0001-0001-0001-000000000001')
BEGIN
    INSERT INTO reference_data.JobCategories (Id, Name, NormalizedName, Description, IsActive, CreatedAt, UpdatedAt)
    VALUES ('A1000000-0001-0001-0001-000000000001', N'Information Technology', N'INFORMATION TECHNOLOGY', N'Software development, infrastructure, and IT-related roles.', 1, GETUTCDATE(), NULL)
END

IF NOT EXISTS (SELECT 1 FROM reference_data.JobCategories WHERE Id = 'A1000000-0001-0001-0001-000000000002')
BEGIN
    INSERT INTO reference_data.JobCategories (Id, Name, NormalizedName, Description, IsActive, CreatedAt, UpdatedAt)
    VALUES ('A1000000-0001-0001-0001-000000000002', N'Design', N'DESIGN', N'UI/UX, graphic, and product design roles.', 1, GETUTCDATE(), NULL)
END");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM reference_data.Skills WHERE Id = 'A2000000-0001-0001-0001-000000000001')
BEGIN
    INSERT INTO reference_data.Skills (Id, Name, NormalizedName, Description, IsActive, CreatedAt, UpdatedAt)
    VALUES ('A2000000-0001-0001-0001-000000000001', N'C#', N'C#', NULL, 1, GETUTCDATE(), NULL)
END

IF NOT EXISTS (SELECT 1 FROM reference_data.Skills WHERE Id = 'A2000000-0001-0001-0001-000000000002')
BEGIN
    INSERT INTO reference_data.Skills (Id, Name, NormalizedName, Description, IsActive, CreatedAt, UpdatedAt)
    VALUES ('A2000000-0001-0001-0001-000000000002', N'JavaScript', N'JAVASCRIPT', NULL, 1, GETUTCDATE(), NULL)
END

IF NOT EXISTS (SELECT 1 FROM reference_data.Skills WHERE Id = 'A2000000-0001-0001-0001-000000000003')
BEGIN
    INSERT INTO reference_data.Skills (Id, Name, NormalizedName, Description, IsActive, CreatedAt, UpdatedAt)
    VALUES ('A2000000-0001-0001-0001-000000000003', N'Angular', N'ANGULAR', NULL, 1, GETUTCDATE(), NULL)
END

IF NOT EXISTS (SELECT 1 FROM reference_data.Skills WHERE Id = 'A2000000-0001-0001-0001-000000000004')
BEGIN
    INSERT INTO reference_data.Skills (Id, Name, NormalizedName, Description, IsActive, CreatedAt, UpdatedAt)
    VALUES ('A2000000-0001-0001-0001-000000000004', N'React', N'REACT', NULL, 1, GETUTCDATE(), NULL)
END

IF NOT EXISTS (SELECT 1 FROM reference_data.Skills WHERE Id = 'A2000000-0001-0001-0001-000000000005')
BEGIN
    INSERT INTO reference_data.Skills (Id, Name, NormalizedName, Description, IsActive, CreatedAt, UpdatedAt)
    VALUES ('A2000000-0001-0001-0001-000000000005', N'Python', N'PYTHON', NULL, 1, GETUTCDATE(), NULL)
END

IF NOT EXISTS (SELECT 1 FROM reference_data.Skills WHERE Id = 'A2000000-0001-0001-0001-000000000006')
BEGIN
    INSERT INTO reference_data.Skills (Id, Name, NormalizedName, Description, IsActive, CreatedAt, UpdatedAt)
    VALUES ('A2000000-0001-0001-0001-000000000006', N'SQL', N'SQL', NULL, 1, GETUTCDATE(), NULL)
END

IF NOT EXISTS (SELECT 1 FROM reference_data.Skills WHERE Id = 'A2000000-0001-0001-0001-000000000007')
BEGIN
    INSERT INTO reference_data.Skills (Id, Name, NormalizedName, Description, IsActive, CreatedAt, UpdatedAt)
    VALUES ('A2000000-0001-0001-0001-000000000007', N'Docker', N'DOCKER', NULL, 1, GETUTCDATE(), NULL)
END

IF NOT EXISTS (SELECT 1 FROM reference_data.Skills WHERE Id = 'A2000000-0001-0001-0001-000000000008')
BEGIN
    INSERT INTO reference_data.Skills (Id, Name, NormalizedName, Description, IsActive, CreatedAt, UpdatedAt)
    VALUES ('A2000000-0001-0001-0001-000000000008', N'AWS', N'AWS', NULL, 1, GETUTCDATE(), NULL)
END

IF NOT EXISTS (SELECT 1 FROM reference_data.Skills WHERE Id = 'A2000000-0001-0001-0001-000000000009')
BEGIN
    INSERT INTO reference_data.Skills (Id, Name, NormalizedName, Description, IsActive, CreatedAt, UpdatedAt)
    VALUES ('A2000000-0001-0001-0001-000000000009', N'Figma', N'FIGMA', NULL, 1, GETUTCDATE(), NULL)
END

IF NOT EXISTS (SELECT 1 FROM reference_data.Skills WHERE Id = 'A2000000-0001-0001-0001-000000000010')
BEGIN
    INSERT INTO reference_data.Skills (Id, Name, NormalizedName, Description, IsActive, CreatedAt, UpdatedAt)
    VALUES ('A2000000-0001-0001-0001-000000000010', N'UX Design', N'UX DESIGN', NULL, 1, GETUTCDATE(), NULL)
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM reference_data.Skills WHERE Id IN ('A2000000-0001-0001-0001-000000000001','A2000000-0001-0001-0001-000000000002','A2000000-0001-0001-0001-000000000003','A2000000-0001-0001-0001-000000000004','A2000000-0001-0001-0001-000000000005','A2000000-0001-0001-0001-000000000006','A2000000-0001-0001-0001-000000000007','A2000000-0001-0001-0001-000000000008','A2000000-0001-0001-0001-000000000009','A2000000-0001-0001-0001-000000000010')");
            migrationBuilder.Sql("DELETE FROM reference_data.JobCategories WHERE Id IN ('A1000000-0001-0001-0001-000000000001','A1000000-0001-0001-0001-000000000002')");
        }
    }
}