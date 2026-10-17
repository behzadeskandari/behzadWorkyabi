namespace IranJob.Modules.Identity.Domain.Constants;

/// <summary>
/// Named authorization policies used throughout the application.
/// Policies are registered in Identity's ServiceCollectionExtensions
/// and referenced via [Authorize(Policy = PolicyNames.XXX)].
/// </summary>
public static class PolicyNames
{
    /// <summary>
    /// Accessible to Admin and SuperAdmin roles.
    /// </summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>
    /// Accessible only to the SuperAdmin role.
    /// </summary>
    public const string SuperAdminOnly = "SuperAdminOnly";
}
