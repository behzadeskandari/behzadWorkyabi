SET NOCOUNT ON;
DECLARE @IranId uniqueidentifier = 'C0A80100-0001-0001-0001-000000000001';

IF NOT EXISTS (SELECT 1 FROM reference_data.Countries WHERE Code = 'IR')
    INSERT INTO reference_data.Countries (Id, Name, NormalizedName, Code, NormalizedCode, IsActive, CreatedAt)
    VALUES ('C0A80100-0001-0001-0001-000000000001', N'ایران', N'ایران', 'IR', 'IR', 1, GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM reference_data.Countries WHERE Code = 'OTH')
    INSERT INTO reference_data.Countries (Id, Name, NormalizedName, Code, NormalizedCode, IsActive, CreatedAt)
    VALUES ('C0A80100-0001-0001-0001-000000000002', N'خارج از ایران', N'خارج از ایران', 'OTH', 'OTH', 1, GETUTCDATE());

SELECT 'Countries' AS Metric, CAST(COUNT(*) AS VARCHAR(10)) AS Count FROM reference_data.Countries;
