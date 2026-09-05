namespace IranJob.Modules.Identity.Domain.Constants;

public static class AuthenticationAuditEventTypes
{
    public const string RegistrationSucceeded = "RegistrationSucceeded";
    public const string LoginSucceeded = "LoginSucceeded";
    public const string LoginFailed = "LoginFailed";
    public const string Logout = "Logout";
    public const string RefreshSucceeded = "RefreshSucceeded";
    public const string RefreshFailed = "RefreshFailed";
    public const string RefreshTokenReuseDetected = "RefreshTokenReuseDetected";
    public const string AccountLocked = "AccountLocked";
}
