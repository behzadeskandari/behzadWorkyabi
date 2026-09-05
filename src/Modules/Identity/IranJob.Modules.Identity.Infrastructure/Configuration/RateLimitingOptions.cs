namespace IranJob.Modules.Identity.Infrastructure.Configuration;

public sealed class RateLimitingOptions
{
    public const string SectionName = "Authentication:RateLimiting";

    public int PermitLimit { get; set; } = 20;

    public int WindowSeconds { get; set; } = 60;
}
