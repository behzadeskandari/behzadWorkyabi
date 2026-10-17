namespace IranJob.BuildingBlocks.Infrastructure.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string ConnectionStringName { get; set; } = "DefaultConnection";

    public bool ApplyMigrationsOnStartup { get; set; }
}

/// <summary>
/// Configuration for candidate resume/CV file storage.
/// </summary>
public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>
    /// Maximum allowed CV file size in bytes (default 5 MB).
    /// </summary>
    public long CvMaxFileSizeBytes { get; set; } = 5L * 1024 * 1024;

    /// <summary>
    /// Allowed file extensions for CV uploads (with leading dot).
    /// </summary>
    public string[] AllowedCvExtensions { get; set; } = [".pdf", ".doc", ".docx"];

    /// <summary>
    /// Allowed MIME content types for CV uploads.
    /// </summary>
    public string[] AllowedCvContentTypes { get; set; } =
    [
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    ];

    /// <summary>
    /// Root folder (relative to ContentRootPath) where candidate CV files are stored.
    /// These files are NOT served via static-file middleware; they are only
    /// served through authorized controller endpoints.
    /// </summary>
    public string CvStoragePath { get; set; } = "cv-storage";
}

