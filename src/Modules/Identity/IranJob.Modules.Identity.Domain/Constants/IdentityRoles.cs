namespace IranJob.Modules.Identity.Domain.Constants;

public static class IdentityRoles
{
    public const string Candidate = "Candidate";
    public const string Employer = "Employer";
    public const string Recruiter = "Recruiter";
    public const string Admin = "Admin";
    public const string SuperAdmin = "SuperAdmin";

    public static readonly IReadOnlyList<string> All =
    [
        Candidate,
        Employer,
        Recruiter,
        Admin,
        SuperAdmin
    ];

    public static readonly IReadOnlyList<string> PublicRegistrationRoles =
    [
        Candidate,
        Employer
    ];
}
