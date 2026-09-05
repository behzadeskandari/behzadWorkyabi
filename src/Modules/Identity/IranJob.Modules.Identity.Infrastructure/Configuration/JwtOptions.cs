namespace IranJob.Modules.Identity.Infrastructure.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Authentication:Jwt";

    public string Issuer { get; set; } = "IranJob";

    public string Audience { get; set; } = "IranJob.Client";

    public string SecretKey { get; set; } = string.Empty;

    public int AccessTokenExpirationMinutes { get; set; } = 15;
}
