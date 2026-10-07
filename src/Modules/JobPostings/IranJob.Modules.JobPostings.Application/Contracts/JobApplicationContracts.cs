namespace IranJob.Modules.JobPostings.Application.Contracts;

public sealed record JobApplicationResponse(
    Guid Id,
    Guid JobId,
    string JobTitle,
    string CompanyName,
    string Status,
    DateTimeOffset AppliedAt,
    DateTimeOffset? UpdatedAt);

public sealed record EmployerApplicationListResponse(
    Guid Id,
    Guid JobId,
    string JobTitle,
    Guid CandidateProfileId,
    string? CandidateHeadline,
    string? CandidateCity,
    string Status,
    DateTimeOffset AppliedAt,
    DateTimeOffset? UpdatedAt);

public sealed record EmployerApplicationResponse(
    Guid Id,
    Guid JobId,
    string JobTitle,
    string? JobDescription,
    Guid CandidateProfileId,
    string? CandidateHeadline,
    string? CandidateBiography,
    string? CandidateCity,
    string? CandidateProvince,
    string? CandidateLinkedInUrl,
    string? CandidateGitHubUrl,
    string? CandidatePortfolioUrl,
    decimal? CandidateExpectedSalary,
    string? CandidateSalaryType,
    string? CandidateEmploymentStatus,
    string? CandidateAvailability,
    string? CandidateMilitaryStatus,
    string Status,
    DateTimeOffset AppliedAt,
    DateTimeOffset? UpdatedAt);
