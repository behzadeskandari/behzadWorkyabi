using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IranJob.Modules.ReferenceData.Infrastructure.Persistence.Migrations
{
    [Migration("20261008000000_AddJobFieldReferenceData")]
    public partial class AddJobFieldReferenceData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmploymentTypes",
                schema: "reference_data",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NormalizedValue = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_EmploymentTypes", x => x.Id); });

            migrationBuilder.CreateTable(
                name: "WorkArrangements",
                schema: "reference_data",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NormalizedValue = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_WorkArrangements", x => x.Id); });

            migrationBuilder.CreateTable(
                name: "SalaryCurrencies",
                schema: "reference_data",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    NormalizedCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_SalaryCurrencies", x => x.Id); });

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM reference_data.EmploymentTypes)
BEGIN
    INSERT INTO reference_data.EmploymentTypes (Id, Value, NormalizedValue, Name, NormalizedName, Description, IsActive, CreatedAt, UpdatedAt)
    VALUES
        ('E0000000-0001-0001-0001-000000000001', N'FullTime', N'FULLTIME', N'تمام‌وقت', N'تمام‌وقت', NULL, 1, GETUTCDATE(), NULL),
        ('E0000000-0001-0001-0001-000000000002', N'PartTime', N'PARTTIME', N'پاره‌وقت', N'پاره‌وقت', NULL, 1, GETUTCDATE(), NULL),
        ('E0000000-0001-0001-0001-000000000003', N'Contract', N'CONTRACT', N'قراردادی', N'قراردادی', NULL, 1, GETUTCDATE(), NULL),
        ('E0000000-0001-0001-0001-000000000004', N'Internship', N'INTERNSHIP', N'کارآموزی', N'کارآموزی', NULL, 1, GETUTCDATE(), NULL),
        ('E0000000-0001-0001-0001-000000000005', N'Temporary', N'TEMPORARY', N'موقت', N'موقت', NULL, 1, GETUTCDATE(), NULL)
END

IF NOT EXISTS (SELECT 1 FROM reference_data.WorkArrangements)
BEGIN
    INSERT INTO reference_data.WorkArrangements (Id, Value, NormalizedValue, Name, NormalizedName, Description, IsActive, CreatedAt, UpdatedAt)
    VALUES
        ('E0000000-0002-0001-0001-000000000001', N'OnSite', N'ONSITE', N'حضوری', N'حضوری', NULL, 1, GETUTCDATE(), NULL),
        ('E0000000-0002-0001-0001-000000000002', N'Hybrid', N'HYBRID', N'ترکیبی', N'ترکیبی', NULL, 1, GETUTCDATE(), NULL),
        ('E0000000-0002-0001-0001-000000000003', N'Remote', N'REMOTE', N'دورکاری', N'دورکاری', NULL, 1, GETUTCDATE(), NULL)
END

IF NOT EXISTS (SELECT 1 FROM reference_data.SalaryCurrencies)
BEGIN
    INSERT INTO reference_data.SalaryCurrencies (Id, Code, NormalizedCode, Name, NormalizedName, Symbol, IsActive, CreatedAt, UpdatedAt)
    VALUES
        ('E0000000-0003-0001-0001-000000000001', N'IRR', N'IRR', N'ریال', N'ریال', N'﷼', 1, GETUTCDATE(), NULL),
        ('E0000000-0003-0001-0001-000000000002', N'IRT', N'IRT', N'تومان', N'تومان', NULL, 1, GETUTCDATE(), NULL),
        ('E0000000-0003-0001-0001-000000000003', N'USD', N'USD', N'دلار آمریکا', N'دلار آمریکا', N'$', 1, GETUTCDATE(), NULL),
        ('E0000000-0003-0001-0001-000000000004', N'EUR', N'EUR', N'یورو', N'یورو', N'€', 1, GETUTCDATE(), NULL)
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM reference_data.EmploymentTypes");
            migrationBuilder.Sql("DELETE FROM reference_data.WorkArrangements");
            migrationBuilder.Sql("DELETE FROM reference_data.SalaryCurrencies");

            migrationBuilder.DropTable(name: "EmploymentTypes", schema: "reference_data");
            migrationBuilder.DropTable(name: "WorkArrangements", schema: "reference_data");
            migrationBuilder.DropTable(name: "SalaryCurrencies", schema: "reference_data");
        }
    }
}