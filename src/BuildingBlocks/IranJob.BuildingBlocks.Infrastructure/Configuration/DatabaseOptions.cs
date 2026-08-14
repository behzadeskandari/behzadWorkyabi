namespace IranJob.BuildingBlocks.Infrastructure.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string ConnectionStringName { get; set; } = "DefaultConnection";

    public bool ApplyMigrationsOnStartup { get; set; }
}
