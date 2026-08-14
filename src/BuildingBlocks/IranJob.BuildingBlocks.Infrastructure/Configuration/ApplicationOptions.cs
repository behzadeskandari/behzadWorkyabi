namespace IranJob.BuildingBlocks.Infrastructure.Configuration;

public sealed class ApplicationOptions
{
    public const string SectionName = "Application";

    public string Name { get; set; } = "IranJob";

    public string Version { get; set; } = "0.1.0";
}
