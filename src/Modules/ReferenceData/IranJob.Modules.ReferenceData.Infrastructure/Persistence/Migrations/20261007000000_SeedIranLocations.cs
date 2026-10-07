using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IranJob.Modules.ReferenceData.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedIranLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM reference_data.Countries WHERE Code = 'IR')
BEGIN
    INSERT INTO reference_data.Countries (Id, Name, NormalizedName, Code, NormalizedCode, IsActive, CreatedAt, UpdatedAt)
    VALUES ('C0A80100-0001-0001-0001-000000000001', N'Ø§ÛŒØ±Ø§Ù†', N'Ø§ÛŒØ±Ø§Ù†', 'IR', 'IR', 1, GETUTCDATE(), NULL)
END

IF NOT EXISTS (SELECT 1 FROM reference_data.Countries WHERE Code = 'OTH')
BEGIN
    INSERT INTO reference_data.Countries (Id, Name, NormalizedName, Code, NormalizedCode, IsActive, CreatedAt, UpdatedAt)
    VALUES ('C0A80100-0001-0001-0001-000000000002', N'خارج از ایران', N'خارج از ایران', 'OTH', 'OTH', 1, GETUTCDATE(), NULL)
END
");

            migrationBuilder.Sql(@"
DECLARE @IranId uniqueidentifier = 'C0A80100-0001-0001-0001-000000000001'
IF NOT EXISTS (SELECT 1 FROM reference_data.Provinces WHERE CountryId = @IranId)
BEGIN
    INSERT INTO reference_data.Provinces (Id, CountryId, Name, NormalizedName, IsActive, CreatedAt, UpdatedAt)
    VALUES
    ('C0A80100-0002-0001-0000-000000000001', @IranId, N'ØªÙ‡Ø±Ø§Ù†', N'ØªÙ‡Ø±Ø§Ù†', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0002-0000-000000000002', @IranId, N'Ø§Ù„Ø¨Ø±Ø²', N'Ø§Ù„Ø¨Ø±Ø²', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0003-0000-000000000003', @IranId, N'Ø§ØµÙÙ‡Ø§Ù†', N'Ø§ØµÙÙ‡Ø§Ù†', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0004-0000-000000000004', @IranId, N'ÙØ§Ø±Ø³', N'ÙØ§Ø±Ø³', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0005-0000-000000000005', @IranId, N'Ø®Ø±Ø§Ø³Ø§Ù† Ø±Ø¶ÙˆÛŒ', N'Ø®Ø±Ø§Ø³Ø§Ù† Ø±Ø¶ÙˆÛŒ', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0006-0000-000000000006', @IranId, N'Ø¢Ø°Ø±Ø¨Ø§ÛŒØ¬Ø§Ù† Ø´Ø±Ù‚ÛŒ', N'Ø¢Ø°Ø±Ø¨Ø§ÛŒØ¬Ø§Ù† Ø´Ø±Ù‚ÛŒ', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0007-0000-000000000007', @IranId, N'Ø¢Ø°Ø±Ø¨Ø§ÛŒØ¬Ø§Ù† ØºØ±Ø¨ÛŒ', N'Ø¢Ø°Ø±Ø¨Ø§ÛŒØ¬Ø§Ù† ØºØ±Ø¨ÛŒ', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0008-0000-000000000008', @IranId, N'Ú©Ù‡Ú¯ÛŒÙ„ÙˆÛŒÙ‡ Ùˆ Ø¨ÙˆÛŒØ±Ø§Ø­Ù…Ø¯', N'Ú©Ù‡Ú¯ÛŒÙ„ÙˆÛŒÙ‡ Ùˆ Ø¨ÙˆÛŒØ±Ø§Ø­Ù…Ø¯', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0009-0000-000000000009', @IranId, N'Ø¨ÙˆØ´Ù‡Ø±', N'Ø¨ÙˆØ´Ù‡Ø±', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-000A-0000-000000000010', @IranId, N'Ú†Ù‡Ø§Ø±Ù…Ø­Ø§Ù„ Ùˆ Ø¨Ø®ØªÛŒØ§Ø±ÛŒ', N'Ú†Ù‡Ø§Ø±Ù…Ø­Ø§Ù„ Ùˆ Ø¨Ø®ØªÛŒØ§Ø±ÛŒ', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-000B-0000-000000000011', @IranId, N'Ù‡Ù…Ø¯Ø§Ù†', N'Ù‡Ù…Ø¯Ø§Ù†', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-000C-0000-000000000012', @IranId, N'Ù‡Ø±Ù…Ø²Ú¯Ø§Ù†', N'Ù‡Ø±Ù…Ø²Ú¯Ø§Ù†', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-000D-0000-000000000013', @IranId, N'Ø®ÙˆØ²Ø³ØªØ§Ù†', N'Ø®ÙˆØ²Ø³ØªØ§Ù†', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-000E-0000-000000000014', @IranId, N'Ù„Ø±Ø³ØªØ§Ù†', N'Ù„Ø±Ø³ØªØ§Ù†', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-000F-0000-000000000015', @IranId, N'Ø®Ø±Ø§Ø³Ø§Ù† Ø´Ù…Ø§Ù„ÛŒ', N'Ø®Ø±Ø§Ø³Ø§Ù† Ø´Ù…Ø§Ù„ÛŒ', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0010-0000-000000000016', @IranId, N'Ø®Ø±Ø§Ø³Ø§Ù† Ø¬Ù†ÙˆØ¨ÛŒ', N'Ø®Ø±Ø§Ø³Ø§Ù† Ø¬Ù†ÙˆØ¨ÛŒ', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0011-0000-000000000017', @IranId, N'Ù‚Ø²ÙˆÛŒÙ†', N'Ù‚Ø²ÙˆÛŒÙ†', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0012-0000-000000000018', @IranId, N'Ù‚Ù…', N'Ù‚Ù…', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0013-0000-000000000019', @IranId, N'Ú©Ø±Ø¯Ø³ØªØ§Ù†', N'Ú©Ø±Ø¯Ø³ØªØ§Ù†', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0014-0000-000000000020', @IranId, N'Ú©Ø±Ù…Ø§Ù†', N'Ú©Ø±Ù…Ø§Ù†', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0015-0000-000000000021', @IranId, N'Ú©Ø±Ù…Ø§Ù†Ø´Ø§Ù‡', N'Ú©Ø±Ù…Ø§Ù†Ø´Ø§Ù‡', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0016-0000-000000000022', @IranId, N'Ú¯Ù„Ø³ØªØ§Ù†', N'Ú¯Ù„Ø³ØªØ§Ù†', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0017-0000-000000000023', @IranId, N'Ú¯ÛŒÙ„Ø§Ù†', N'Ú¯ÛŒÙ„Ø§Ù†', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0018-0000-000000000024', @IranId, N'Ù…Ø§Ø²Ù†Ø¯Ø±Ø§Ù†', N'Ù…Ø§Ø²Ù†Ø¯Ø±Ø§Ù†', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0019-0000-000000000025', @IranId, N'Ù…Ø±Ú©Ø²ÛŒ', N'Ù…Ø±Ú©Ø²ÛŒ', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-001A-0000-000000000026', @IranId, N'Ø§Ø±Ø¯Ø¨ÛŒÙ„', N'Ø§Ø±Ø¯Ø¨ÛŒÙ„', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-001B-0000-000000000027', @IranId, N'Ø§ÛŒÙ„Ø§Ù…', N'Ø§ÛŒÙ„Ø§Ù…', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-001C-0000-000000000028', @IranId, N'ÛŒØ²Ø¯', N'ÛŒØ²Ø¯', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-001D-0000-000000000029', @IranId, N'Ø³Ù…Ù†Ø§Ù†', N'Ø³Ù…Ù†Ø§Ù†', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-001E-0000-000000000030', @IranId, N'Ø³ÛŒØ³ØªØ§Ù† Ùˆ Ø¨Ù„ÙˆÚ†Ø³ØªØ§Ù†', N'Ø³ÛŒØ³ØªØ§Ù† Ùˆ Ø¨Ù„ÙˆÚ†Ø³ØªØ§Ù†', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-001F-0000-000000000031', @IranId, N'Ø²Ù†Ø¬Ø§Ù†', N'Ø²Ù†Ø¬Ø§Ù†', 1, GETUTCDATE(), NULL)
END
");

            migrationBuilder.Sql(@"
DECLARE @IranId uniqueidentifier = 'C0A80100-0001-0001-0001-000000000001'
IF NOT EXISTS (SELECT COUNT(*) FROM reference_data.Cities WHERE ProvinceId IN (SELECT Id FROM reference_data.Provinces WHERE CountryId = @IranId))
BEGIN
    INSERT INTO reference_data.Cities (Id, ProvinceId, Name, NormalizedName, IsActive, CreatedAt, UpdatedAt)
    VALUES
    ('C0A80100-0003-0001-0000-000000000101', 'C0A80100-0002-0001-0000-000000000001', N'ØªÙ‡Ø±Ø§Ù†', N'ØªÙ‡Ø±Ø§Ù†', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0001-0000-000000000102', 'C0A80100-0002-0001-0000-000000000001', N'Ø´Ù…ÛŒØ±Ø§Ù†Ø§Øª', N'Ø´Ù…ÛŒØ±Ø§Ù†Ø§Øª', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0001-0000-000000000103', 'C0A80100-0002-0001-0000-000000000001', N'Ø¯Ù…Ø§ÙˆÙ†Ø¯', N'Ø¯Ù…Ø§ÙˆÙ†Ø¯', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0001-0000-000000000104', 'C0A80100-0002-0001-0000-000000000001', N'Ø±ÛŒ', N'Ø±ÛŒ', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0002-0000-000000000201', 'C0A80100-0002-0002-0000-000000000002', N'Ú©Ø±Ø¬', N'Ú©Ø±Ø¬', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0002-0000-000000000202', 'C0A80100-0002-0002-0000-000000000002', N'Ø³Ø§ÙˆÙ‡', N'Ø³Ø§ÙˆÙ‡', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0003-0000-000000000301', 'C0A80100-0002-0003-0000-000000000003', N'Ø§ØµÙÙ‡Ø§Ù†', N'Ø§ØµÙÙ‡Ø§Ù†', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0003-0000-000000000302', 'C0A80100-0002-0003-0000-000000000003', N'Ú©Ø§Ø´Ø§Ù†', N'Ú©Ø§Ø´Ø§Ù†', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0004-0000-000000000401', 'C0A80100-0002-0004-0000-000000000004', N'Ø´ÛŒØ±Ø§Ø²', N'Ø´ÛŒØ±Ø§Ø²', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0004-0000-000000000402', 'C0A80100-0002-0004-0000-000000000004', N'Ù„Ø§Ø±', N'Ù„Ø§Ø±', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0005-0000-000000000501', 'C0A80100-0002-0005-0000-000000000005', N'Ù…Ø´Ù‡Ø¯', N'Ù…Ø´Ù‡Ø¯', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0005-0000-000000000502', 'C0A80100-0002-0005-0000-000000000005', N'Ù†ÛŒØ´Ø§Ø¨ÙˆØ±', N'Ù†ÛŒØ´Ø§Ø¨ÙˆØ±', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0006-0000-000000000601', 'C0A80100-0002-0006-0000-000000000006', N'ØªØ¨Ø±ÛŒØ²', N'ØªØ¨Ø±ÛŒØ²', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0007-0000-000000000701', 'C0A80100-0002-0007-0000-000000000007', N'Ø§Ø±ÙˆÙ…ÛŒÙ‡', N'Ø§Ø±ÙˆÙ…ÛŒÙ‡', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0008-0000-000000000801', 'C0A80100-0002-0008-0000-000000000008', N'ÛŒØ§Ø³ÙˆØ¬', N'ÛŒØ§Ø³ÙˆØ¬', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0009-0000-000000000901', 'C0A80100-0002-0009-0000-000000000009', N'Ø¨ÙˆØ´Ù‡Ø±', N'Ø¨ÙˆØ´Ù‡Ø±', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-000A-0000-000000001001', 'C0A80100-0002-000A-0000-000000000010', N'Ø´Ù‡Ø±Ú©Ø±Ø¯', N'Ø´Ù‡Ø±Ú©Ø±Ø¯', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-000B-0000-000000001101', 'C0A80100-0002-000B-0000-000000000011', N'Ù‡Ù…Ø¯Ø§Ù†', N'Ù‡Ù…Ø¯Ø§Ù†', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-000C-0000-000000001201', 'C0A80100-0002-000C-0000-000000000012', N'Ø¨Ù†Ø¯Ø±Ø¹Ø¨Ø§Ø³', N'Ø¨Ù†Ø¯Ø±Ø¹Ø¨Ø§Ø³', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-000D-0000-000000001301', 'C0A80100-0002-000D-0000-000000000013', N'Ø§Ù‡ÙˆØ§Ø²', N'Ø§Ù‡ÙˆØ§Ø²', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-000D-0000-000000001302', 'C0A80100-0002-000D-0000-000000000013', N'Ø®Ø±Ù…Ø´Ù‡Ø±', N'Ø®Ø±Ù…Ø´Ù‡Ø±', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-000E-0000-000000001401', 'C0A80100-0002-000E-0000-000000000014', N'Ø®Ø±Ù…â€ŒØ¢Ø¨Ø§Ø¯', N'Ø®Ø±Ù…â€ŒØ¢Ø¨Ø§Ø¯', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-000F-0000-000000001501', 'C0A80100-0002-000F-0000-000000000015', N'Ø¨Ø¬Ù†ÙˆØ±Ø¯', N'Ø¨Ø¬Ù†ÙˆØ±Ø¯', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0010-0000-000000001601', 'C0A80100-0002-0010-0000-000000000016', N'Ø¨ÛŒØ±Ø¬Ù†Ø¯', N'Ø¨ÛŒØ±Ø¬Ù†Ø¯', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0011-0000-000000001701', 'C0A80100-0002-0011-0000-000000000017', N'Ù‚Ø²ÙˆÛŒÙ†', N'Ù‚Ø²ÙˆÛŒÙ†', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0012-0000-000000001801', 'C0A80100-0002-0012-0000-000000000018', N'Ù‚Ù…', N'Ù‚Ù…', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0013-0000-000000001901', 'C0A80100-0002-0013-0000-000000000019', N'Ø³Ù†Ù†Ø¯Ø¬', N'Ø³Ù†Ù†Ø¯Ø¬', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0014-0000-000000002001', 'C0A80100-0002-0014-0000-000000000020', N'Ú©Ø±Ù…Ø§Ù†', N'Ú©Ø±Ù…Ø§Ù†', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0015-0000-000000002101', 'C0A80100-0002-0015-0000-000000000021', N'Ú©Ø±Ù…Ø§Ù†Ø´Ø§Ù‡', N'Ú©Ø±Ù…Ø§Ù†Ø´Ø§Ù‡', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0016-0000-000000002201', 'C0A80100-0002-0016-0000-000000000022', N'Ú¯Ø±Ú¯Ø§Ù†', N'Ú¯Ø±Ú¯Ø§Ù†', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0017-0000-000000002301', 'C0A80100-0002-0017-0000-000000000023', N'Ø±Ø´Øª', N'Ø±Ø´Øª', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0018-0000-000000002401', 'C0A80100-0002-0018-0000-000000000024', N'Ø³Ø§Ø±ÛŒ', N'Ø³Ø§Ø±ÛŒ', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0019-0000-000000002501', 'C0A80100-0002-0019-0000-000000000025', N'Ø§Ø±Ø§Ú©', N'Ø§Ø±Ø§Ú©', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-001A-0000-000000002601', 'C0A80100-0002-001A-0000-000000000026', N'Ø§Ø±Ø¯Ø¨ÛŒÙ„', N'Ø§Ø±Ø¯Ø¨ÛŒÙ„', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-001B-0000-000000002701', 'C0A80100-0002-001B-0000-000000000027', N'Ø§ÛŒÙ„Ø§Ù…', N'Ø§ÛŒÙ„Ø§Ù…', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-001C-0000-000000002801', 'C0A80100-0002-001C-0000-000000000028', N'ÛŒØ²Ø¯', N'ÛŒØ²Ø¯', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-001D-0000-000000002901', 'C0A80100-0002-001D-0000-000000000029', N'Ø³Ù…Ù†Ø§Ù†', N'Ø³Ù…Ù†Ø§Ù†', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-001E-0000-000000003001', 'C0A80100-0002-001E-0000-000000000030', N'Ø²Ø§Ù‡Ø¯Ø§Ù†', N'Ø²Ø§Ù‡Ø¯Ø§Ù†', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-001F-0000-000000003101', 'C0A80100-0002-001F-0000-000000000031', N'Ø²Ù†Ø¬Ø§Ù†', N'Ø²Ù†Ø¬Ø§Ù†', 1, GETUTCDATE(), NULL)
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM reference_data.Cities WHERE ProvinceId IN (SELECT Id FROM reference_data.Provinces WHERE CountryId IN (SELECT Id FROM reference_data.Countries WHERE Code IN ('IR', 'OTH')))");
            migrationBuilder.Sql("DELETE FROM reference_data.Provinces WHERE CountryId IN (SELECT Id FROM reference_data.Countries WHERE Code IN ('IR', 'OTH'))");
            migrationBuilder.Sql("DELETE FROM reference_data.Countries WHERE Code IN ('IR', 'OTH')");
        }
    }
}
