namespace IranJob.Modules.Candidates.Application.Abstractions;

public interface ICandidateProfileService
{
    Task<CandidateProfileResult> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<CandidateProfileResult> CreateProfileAsync(Guid userId, CandidateProfileRequest request, CancellationToken cancellationToken = default);

    Task<CandidateProfileResult> UpdateProfileAsync(Guid userId, CandidateProfileRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Payload used by both create (POST) and update (PUT). All fields are optional so a
/// candidate can build the profile progressively. Ownership is always the authenticated
/// user; the request carries no user identifier.
/// Account first name, last name, email, and phone belong to Identity and are not stored here.
/// </summary>
public sealed record CandidateProfileRequest(
    string? Headline,
    string? Biography,
    DateOnly? DateOfBirth,
    string? Gender,
    string? City,
    string? Province,
    string? LinkedInUrl,
    string? GitHubUrl,
    string? PortfolioUrl,
    decimal? ExpectedSalary,
    string? SalaryType,
    string? EmploymentStatus,
    string? Availability,
    string? MilitaryStatus);

public sealed record CandidateProfileResult(
    Guid Id,
    Guid UserId,
    string? Headline,
    string? Biography,
    DateOnly? DateOfBirth,
    string? Gender,
    string? City,
    string? Province,
    string? LinkedInUrl,
    string? GitHubUrl,
    string? PortfolioUrl,
    decimal? ExpectedSalary,
    string? SalaryType,
    string? EmploymentStatus,
    string? Availability,
    string? MilitaryStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
