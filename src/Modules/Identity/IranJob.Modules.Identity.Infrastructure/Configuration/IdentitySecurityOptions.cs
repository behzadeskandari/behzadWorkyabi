namespace IranJob.Modules.Identity.Infrastructure.Configuration;

public sealed class IdentitySecurityOptions
{
    public const string SectionName = "Authentication:Identity";

    public int MaxFailedAccessAttempts { get; set; } = 5;

    public int LockoutMinutes { get; set; } = 15;

    public int RefreshTokenExpirationDays { get; set; } = 7;

    public string RefreshTokenCookieName { get; set; } = "iranjob_refresh_token";

    public string CsrfCookieName { get; set; } = "iranjob_csrf";

    public string CsrfHeaderName { get; set; } = "X-CSRF-TOKEN";
}
