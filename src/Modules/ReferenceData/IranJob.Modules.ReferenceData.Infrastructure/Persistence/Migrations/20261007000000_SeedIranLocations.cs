using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IranJob.Modules.ReferenceData.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [Migration("20261007000000_SeedIranLocations")]
    public partial class SeedIranLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM reference_data.Countries WHERE Code = 'IR')
BEGIN
    INSERT INTO reference_data.Countries (Id, Name, NormalizedName, Code, NormalizedCode, IsActive, CreatedAt, UpdatedAt)
    VALUES ('C0A80100-0001-0001-0001-000000000001', N'ایران', N'ایران', 'IR', 'IR', 1, GETUTCDATE(), NULL)
END

IF NOT EXISTS (SELECT 1 FROM reference_data.Countries WHERE Code = 'OTH')
BEGIN
    INSERT INTO reference_data.Countries (Id, Name, NormalizedName, Code, NormalizedCode, IsActive, CreatedAt, UpdatedAt)
    VALUES ('C0A80100-0001-0001-0001-000000000002', N'خارج از ایران', N'خارج از ایران', 'OTH', 'OTH', 1, GETUTCDATE(), NULL)
END

-- Recovery: fix any records with empty names caused by a double-encoding corruption
UPDATE reference_data.Countries SET Name = N'ایران', NormalizedName = N'ایران' WHERE Code IN ('IR','OTH') AND (Name IS NULL OR Name = N'')
");

            migrationBuilder.Sql(@"
DECLARE @IranId uniqueidentifier = 'C0A80100-0001-0001-0001-000000000001'
IF NOT EXISTS (SELECT 1 FROM reference_data.Provinces WHERE CountryId = @IranId)
BEGIN
    INSERT INTO reference_data.Provinces (Id, CountryId, Name, NormalizedName, IsActive, CreatedAt, UpdatedAt)
    VALUES
    ('C0A80100-0002-0001-0000-000000000001', @IranId, N'تهران', N'تهران', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0002-0000-000000000002', @IranId, N'آذربایجان شرقی', N'آذربایجان شرقی', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0003-0000-000000000003', @IranId, N'آذربایجان غربی', N'آذربایجان غربی', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0004-0000-000000000004', @IranId, N'اصفهان', N'اصفهان', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0005-0000-000000000005', @IranId, N'خراسان رضوی', N'خراسان رضوی', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0006-0000-000000000006', @IranId, N'فارس', N'فارس', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0007-0000-000000000007', @IranId, N'خوزستان', N'خوزستان', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0008-0000-000000000008', @IranId, N'خراسان جنوبی', N'خراسان جنوبی', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0009-0000-000000000009', @IranId, N'خراسان شمالی', N'خراسان شمالی', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-000A-0000-000000000010', @IranId, N'مازندران', N'مازندران', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-000B-0000-000000000011', @IranId, N'گیلان', N'گیلان', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-000C-0000-000000000012', @IranId, N'گلستان', N'گلستان', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-000D-0000-000000000013', @IranId, N'کرمان', N'کرمان', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-000E-0000-000000000014', @IranId, N'کرمانشاه', N'کرمانشاه', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-000F-0000-000000000015', @IranId, N'لرستان', N'لرستان', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0010-0000-000000000016', @IranId, N'کردستان', N'کردستان', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0011-0000-000000000017', @IranId, N'کهگیلویه و بویری', N'کهگیلویه و بویری', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0012-0000-000000000018', @IranId, N'بوشهر', N'بوشهر', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0013-0000-000000000019', @IranId, N'هرمزگان', N'هرمزگان', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0014-0000-000000000020', @IranId, N'همدان', N'همدان', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0015-0000-000000000021', @IranId, N'مرکزی', N'مرکزی', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0016-0000-000000000022', @IranId, N'زنجان', N'زنجان', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0017-0000-000000000023', @IranId, N'سمنان', N'سمنان', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0018-0000-000000000024', @IranId, N'سیستان و بلوچستان', N'سیستان و بلوچستان', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-0019-0000-000000000025', @IranId, N'قزوین', N'قزوین', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-001A-0000-000000000026', @IranId, N'قم', N'قم', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-001B-0000-000000000027', @IranId, N'اردبیل', N'اردبیل', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-001C-0000-000000000028', @IranId, N'ایلام', N'ایلام', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-001D-0000-000000000029', @IranId, N'یزد', N'یزد', 1, GETUTCDATE(), NULL),
    ('C0A80100-0002-001E-0000-000000000030', @IranId, N'چهارمحال و بختیاری', N'چهارمحال و بختیاری', 1, GETUTCDATE(), NULL)
END
");

            migrationBuilder.Sql(@"
-- Recovery: fix Province records with empty names caused by double-encoding corruption
UPDATE reference_data.Provinces SET Name = N'تهران', NormalizedName = N'تهران' WHERE Id = 'C0A80100-0002-0001-0000-000000000001' AND (Name IS NULL OR Name = N'')
UPDATE reference_data.Provinces SET Name = N'آذربایجان شرقی', NormalizedName = N'آذربایجان شرقی' WHERE Id = 'C0A80100-0002-0002-0000-000000000002' AND (Name IS NULL OR Name = N'')
UPDATE reference_data.Provinces SET Name = N'آذربایجان غربی', NormalizedName = N'آذربایجان غربی' WHERE Id = 'C0A80100-0002-0003-0000-000000000003' AND (Name IS NULL OR Name = N'')
UPDATE reference_data.Provinces SET Name = N'اصفهان', NormalizedName = N'اصفهان' WHERE Id = 'C0A80100-0002-0004-0000-000000000004' AND (Name IS NULL OR Name = N'')
UPDATE reference_data.Provinces SET Name = N'خراسان رضوی', NormalizedName = N'خراسان رضوی' WHERE Id = 'C0A80100-0002-0005-0000-000000000005' AND (Name IS NULL OR Name = N'')
UPDATE reference_data.Provinces SET Name = N'فارس', NormalizedName = N'فارس' WHERE Id = 'C0A80100-0002-0006-0000-000000000006' AND (Name IS NULL OR Name = N'')
UPDATE reference_data.Provinces SET Name = N'خوزستان', NormalizedName = N'خوزستان' WHERE Id = 'C0A80100-0002-0007-0000-000000000007' AND (Name IS NULL OR Name = N'')
UPDATE reference_data.Provinces SET Name = N'خراسان جنوبی', NormalizedName = N'خراسان جنوبی' WHERE Id = 'C0A80100-0002-0008-0000-000000000008' AND (Name IS NULL OR Name = N'')
UPDATE reference_data.Provinces SET Name = N'خراسان شمالی', NormalizedName = N'خراسان شمالی' WHERE Id = 'C0A80100-0002-0009-0000-000000000009' AND (Name IS NULL OR Name = N'')
UPDATE reference_data.Provinces SET Name = N'مازندران', NormalizedName = N'مازندران' WHERE Id = 'C0A80100-0002-000A-0000-000000000010' AND (Name IS NULL OR Name = N'')
UPDATE reference_data.Provinces SET Name = N'گیلان', NormalizedName = N'گیلان' WHERE Id = 'C0A80100-0002-000B-0000-000000000011' AND (Name IS NULL OR Name = N'')
UPDATE reference_data.Provinces SET Name = N'گلستان', NormalizedName = N'گلستان' WHERE Id = 'C0A80100-0002-000C-0000-000000000012' AND (Name IS NULL OR Name = N'')
UPDATE reference_data.Provinces SET Name = N'کرمان', NormalizedName = N'کرمان' WHERE Id = 'C0A80100-0002-000D-0000-000000000013' AND (Name IS NULL OR Name = N'')
UPDATE reference_data.Provinces SET Name = N'کرمانشاه', NormalizedName = N'کرمانشاه' WHERE Id = 'C0A80100-0002-000E-0000-000000000014' AND (Name IS NULL OR Name = N'')
UPDATE reference_data.Provinces SET Name = N'لرستان', NormalizedName = N'لرستان' WHERE Id = 'C0A80100-0002-000F-0000-000000000015' AND (Name IS NULL OR Name = N'')
UPDATE reference_data.Provinces SET Name = N'کردستان', NormalizedName = N'کردستان' WHERE Id = 'C0A80100-0002-0010-0000-000000000016' AND (Name IS NULL OR Name = N'')
UPDATE reference_data.Provinces SET Name = N'کهگیلویه و بویری', NormalizedName = N'کهگیلویه و بویری' WHERE Id = 'C0A80100-0002-0011-0000-000000000017' AND (Name IS NULL OR Name = N'')
UPDATE reference_data.Provinces SET Name = N'بوشهر', NormalizedName = N'بوشهر' WHERE Id = 'C0A80100-0002-0012-0000-000000000018' AND (Name IS NULL OR Name = N'')
UPDATE reference_data.Provinces SET Name = N'هرمزگان', NormalizedName = N'هرمزگان' WHERE Id = 'C0A80100-0002-0013-0000-000000000019' AND (Name IS NULL OR Name = N'')
UPDATE reference_data.Provinces SET Name = N'همدان', NormalizedName = N'همدان' WHERE Id = 'C0A80100-0002-0014-0000-000000000020' AND (Name IS NULL OR Name = N'')
UPDATE reference_data.Provinces SET Name = N'مرکزی', NormalizedName = N'مرکزی' WHERE Id = 'C0A80100-0002-0015-0000-000000000021' AND (Name IS NULL OR Name = N'')
UPDATE reference_data.Provinces SET Name = N'زنجان', NormalizedName = N'زنجان' WHERE Id = 'C0A80100-0002-0016-0000-000000000022' AND (Name IS NULL OR Name = N'')
UPDATE reference_data.Provinces SET Name = N'سمنان', NormalizedName = N'سمنان' WHERE Id = 'C0A80100-0002-0017-0000-000000000023' AND (Name IS NULL OR Name = N'')
UPDATE reference_data.Provinces SET Name = N'سیستان و بلوچستان', NormalizedName = N'سیستان و بلوچستان' WHERE Id = 'C0A80100-0002-0018-0000-000000000024' AND (Name IS NULL OR Name = N'')
UPDATE reference_data.Provinces SET Name = N'قزوین', NormalizedName = N'قزوین' WHERE Id = 'C0A80100-0002-0019-0000-000000000025' AND (Name IS NULL OR Name = N'')
UPDATE reference_data.Provinces SET Name = N'قم', NormalizedName = N'قم' WHERE Id = 'C0A80100-0002-001A-0000-000000000026' AND (Name IS NULL OR Name = N'')
UPDATE reference_data.Provinces SET Name = N'اردبیل', NormalizedName = N'اردبیل' WHERE Id = 'C0A80100-0002-001B-0000-000000000027' AND (Name IS NULL OR Name = N'')
UPDATE reference_data.Provinces SET Name = N'ایلام', NormalizedName = N'ایلام' WHERE Id = 'C0A80100-0002-001C-0000-000000000028' AND (Name IS NULL OR Name = N'')
UPDATE reference_data.Provinces SET Name = N'یزد', NormalizedName = N'یزد' WHERE Id = 'C0A80100-0002-001D-0000-000000000029' AND (Name IS NULL OR Name = N'')
UPDATE reference_data.Provinces SET Name = N'چهارمحال و بختیاری', NormalizedName = N'چهارمحال و بختیاری' WHERE Id = 'C0A80100-0002-001E-0000-000000000030' AND (Name IS NULL OR Name = N'')

-- Remove the orphaned 31st province (no valid Iranian province name maps to this ID)
DELETE FROM reference_data.Cities WHERE ProvinceId = 'C0A80100-0002-001F-0000-000000000031'
DELETE FROM reference_data.Provinces WHERE Id = 'C0A80100-0002-001F-0000-000000000031' AND (Name IS NULL OR Name = N'')
");

            migrationBuilder.Sql(@"
DECLARE @IranId uniqueidentifier = 'C0A80100-0001-0001-0001-000000000001'
IF NOT EXISTS (SELECT COUNT(*) FROM reference_data.Cities WHERE ProvinceId IN (SELECT Id FROM reference_data.Provinces WHERE CountryId = @IranId))
BEGIN
    INSERT INTO reference_data.Cities (Id, ProvinceId, Name, NormalizedName, IsActive, CreatedAt, UpdatedAt)
    VALUES
    ('C0A80100-0003-0001-0000-000000000101', 'C0A80100-0002-0001-0000-000000000001', N'تهران', N'تهران', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0001-0000-000000000102', 'C0A80100-0002-0001-0000-000000000001', N'کرج', N'کرج', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0001-0000-000000000103', 'C0A80100-0002-0001-0000-000000000001', N'شمیرانات', N'شمیرانات', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0001-0000-000000000104', 'C0A80100-0002-0001-0000-000000000001', N'ری', N'ری', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0002-0000-000000000201', 'C0A80100-0002-0002-0000-000000000002', N'تبریز', N'تبریز', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0002-0000-000000000202', 'C0A80100-0002-0002-0000-000000000002', N'بناب', N'بناب', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0003-0000-000000000301', 'C0A80100-0002-0003-0000-000000000003', N'ارومیه', N'ارومیه', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0003-0000-000000000302', 'C0A80100-0002-0003-0000-000000000003', N'مهاباد', N'مهاباد', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0004-0000-000000000401', 'C0A80100-0002-0004-0000-000000000004', N'اصفهان', N'اصفهان', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0004-0000-000000000402', 'C0A80100-0002-0004-0000-000000000004', N'فریدون‌شهر', N'فریدون‌شهر', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0005-0000-000000000501', 'C0A80100-0002-0005-0000-000000000005', N'مشهد', N'مشهد', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0005-0000-000000000502', 'C0A80100-0002-0005-0000-000000000005', N'نیشابور', N'نیشابور', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0006-0000-000000000601', 'C0A80100-0002-0006-0000-000000000006', N'شیراز', N'شیراز', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0007-0000-000000000701', 'C0A80100-0002-0007-0000-000000000007', N'اهواز', N'اهواز', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0008-0000-000000000801', 'C0A80100-0002-0008-0000-000000000008', N'بیرجند', N'بیرجند', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0009-0000-000000000901', 'C0A80100-0002-0009-0000-000000000009', N'بجنورد', N'بجنورد', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-000A-0000-000000001001', 'C0A80100-0002-000A-0000-000000000010', N'ساری', N'ساری', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-000B-0000-000000001101', 'C0A80100-0002-000B-0000-000000000011', N'رشت', N'رشت', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-000C-0000-000000001201', 'C0A80100-0002-000C-0000-000000000012', N'گرگان', N'گرگان', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-000D-0000-000000001301', 'C0A80100-0002-000D-0000-000000000013', N'کرمان', N'کرمان', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-000D-0000-000000001302', 'C0A80100-0002-000D-0000-000000000013', N'بم', N'بم', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-000E-0000-000000001401', 'C0A80100-0002-000E-0000-000000000014', N'کرمانشاه', N'کرمانشاه', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-000F-0000-000000001501', 'C0A80100-0002-000F-0000-000000000015', N'خرم‌آباد', N'خرم‌آباد', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0010-0000-000000001601', 'C0A80100-0002-0010-0000-000000000016', N'سنندج', N'سنندج', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0011-0000-000000001701', 'C0A80100-0002-0011-0000-000000000017', N'یاسوج', N'یاسوج', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0012-0000-000000001801', 'C0A80100-0002-0012-0000-000000000018', N'بوشهر', N'بوشهر', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0013-0000-000000001901', 'C0A80100-0002-0013-0000-000000000019', N'بندرعباس', N'بندرعباس', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0014-0000-000000002001', 'C0A80100-0002-0014-0000-000000000020', N'همدان', N'همدان', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0015-0000-000000002101', 'C0A80100-0002-0015-0000-000000000021', N'اراک', N'اراک', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0016-0000-000000002201', 'C0A80100-0002-0016-0000-000000000022', N'زنجان', N'زنجان', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0017-0000-000000002301', 'C0A80100-0002-0017-0000-000000000023', N'سمنان', N'سمنان', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0018-0000-000000002401', 'C0A80100-0002-0018-0000-000000000024', N'زاهدان', N'زاهدان', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-0019-0000-000000002501', 'C0A80100-0002-0019-0000-000000000025', N'قزوین', N'قزوین', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-001A-0000-000000002601', 'C0A80100-0002-001A-0000-000000000026', N'قم', N'قم', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-001B-0000-000000002701', 'C0A80100-0002-001B-0000-000000000027', N'اردبیل', N'اردبیل', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-001C-0000-000000002801', 'C0A80100-0002-001C-0000-000000000028', N'ایلام', N'ایلام', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-001D-0000-000000002901', 'C0A80100-0002-001D-0000-000000000029', N'یزد', N'یزد', 1, GETUTCDATE(), NULL),
    ('C0A80100-0003-001E-0000-000000003001', 'C0A80100-0002-001E-0000-000000000030', N'شهرکرد', N'شهرکرد', 1, GETUTCDATE(), NULL)
END
");

            migrationBuilder.Sql(@"
-- Recovery: fix City records with empty names caused by double-encoding corruption
UPDATE c
SET Name = v.Name, NormalizedName = v.NormalizedName
FROM reference_data.Cities c
JOIN (VALUES
    ('C0A80100-0003-0001-0000-000000000101', N'تهران', N'تهران'),
    ('C0A80100-0003-0001-0000-000000000102', N'کرج', N'کرج'),
    ('C0A80100-0003-0001-0000-000000000103', N'شمیرانات', N'شمیرانات'),
    ('C0A80100-0003-0001-0000-000000000104', N'ری', N'ری'),
    ('C0A80100-0003-0002-0000-000000000201', N'تبریز', N'تبریز'),
    ('C0A80100-0003-0002-0000-000000000202', N'بناب', N'بناب'),
    ('C0A80100-0003-0003-0000-000000000301', N'ارومیه', N'ارومیه'),
    ('C0A80100-0003-0003-0000-000000000302', N'مهاباد', N'مهاباد'),
    ('C0A80100-0003-0004-0000-000000000401', N'اصفهان', N'اصفهان'),
    ('C0A80100-0003-0004-0000-000000000402', N'فریدون‌شهر', N'فریدون‌شهر'),
    ('C0A80100-0003-0005-0000-000000000501', N'مشهد', N'مشهد'),
    ('C0A80100-0003-0005-0000-000000000502', N'نیشابور', N'نیشابور'),
    ('C0A80100-0003-0006-0000-000000000601', N'شیراز', N'شیراز'),
    ('C0A80100-0003-0007-0000-000000000701', N'اهواز', N'اهواز'),
    ('C0A80100-0003-0008-0000-000000000801', N'بیرجند', N'بیرجند'),
    ('C0A80100-0003-0009-0000-000000000901', N'بجنورد', N'بجنورد'),
    ('C0A80100-0003-000A-0000-000000001001', N'ساری', N'ساری'),
    ('C0A80100-0003-000B-0000-000000001101', N'رشت', N'رشت'),
    ('C0A80100-0003-000C-0000-000000001201', N'گرگان', N'گرگان'),
    ('C0A80100-0003-000D-0000-000000001301', N'کرمان', N'کرمان'),
    ('C0A80100-0003-000D-0000-000000001302', N'بم', N'بم'),
    ('C0A80100-0003-000E-0000-000000001401', N'کرمانشاه', N'کرمانشاه'),
    ('C0A80100-0003-000F-0000-000000001501', N'خرم‌آباد', N'خرم‌آباد'),
    ('C0A80100-0003-0010-0000-000000001601', N'سنندج', N'سنندج'),
    ('C0A80100-0003-0011-0000-000000001701', N'یاسوج', N'یاسوج'),
    ('C0A80100-0003-0012-0000-000000001801', N'بوشهر', N'بوشهر'),
    ('C0A80100-0003-0013-0000-000000001901', N'بندرعباس', N'بندرعباس'),
    ('C0A80100-0003-0014-0000-000000002001', N'همدان', N'همدان'),
    ('C0A80100-0003-0015-0000-000000002101', N'اراک', N'اراک'),
    ('C0A80100-0003-0016-0000-000000002201', N'زنجان', N'زنجان'),
    ('C0A80100-0003-0017-0000-000000002301', N'سمنان', N'سمنان'),
    ('C0A80100-0003-0018-0000-000000002401', N'زاهدان', N'زاهدان'),
    ('C0A80100-0003-0019-0000-000000002501', N'قزوین', N'قزوین'),
    ('C0A80100-0003-001A-0000-000000002601', N'قم', N'قم'),
    ('C0A80100-0003-001B-0000-000000002701', N'اردبیل', N'اردبیل'),
    ('C0A80100-0003-001C-0000-000000002801', N'ایلام', N'ایلام'),
    ('C0A80100-0003-001D-0000-000000002901', N'یزد', N'یزد'),
    ('C0A80100-0003-001E-0000-000000003001', N'شهرکرد', N'شهرکرد')
) AS v(Id, Name, NormalizedName) ON c.Id = v.Id
WHERE c.Name IS NULL OR c.Name = N''

-- Remove the orphaned city for the orphaned 31st province
DELETE FROM reference_data.Cities WHERE ProvinceId = 'C0A80100-0002-001F-0000-000000000031'
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
