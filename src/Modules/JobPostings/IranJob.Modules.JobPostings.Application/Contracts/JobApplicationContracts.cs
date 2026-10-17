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

public sealed record EmployerCvAccessResponse(
    Guid ApplicationId,
    Guid CandidateProfileId,
    string? CandidateHeadline,
    Stream CvContent,
    string CvContentType,
    string CvDownloadName);

/// <summary>
/// Self-contained resume view returned to an employer for one of their
/// job applications. Uses local DTOs (JobPostings.Application does not
/// reference Candidates.Application) so the two modules stay decoupled.
/// </summary>
public sealed record EmployerResumeViewResponse(
    Guid ApplicationId,
    Guid CandidateProfileId,
    string? CandidateHeadline,
    string Status,
    int CompletenessScore,
    bool HasCvFile,
    EmployerResumeSectionView[] WorkExperiences,
    EmployerResumeSectionView[] Educations,
    EmployerResumeSkillView[] Skills,
    EmployerResumeLanguageView[] Languages,
    EmployerResumeProjectView[] Projects,
    EmployerResumeCertificationView[] Certifications,
    EmployerResumePortfolioLinkView[] PortfolioLinks);

public sealed record EmployerResumeSectionView(
    Guid Id,
    string PrimaryLabel,
    string SecondaryLabel,
    string? TertiaryLabel,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool IsCurrent,
    int DurationMonths,
    int SortOrder);

public sealed record EmployerResumeSkillView(
    Guid Id,
    Guid SkillId,
    string SkillName,
    string? ProficiencyLevel,
    int? YearsOfExperience,
    int SortOrder);

public sealed record EmployerResumeLanguageView(
    Guid Id,
    string LanguageName,
    string ProficiencyLevel,
    int SortOrder);

public sealed record EmployerResumeProjectView(
    Guid Id,
    string Title,
    string? Description,
    string? Url,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool IsCurrent,
    int SortOrder);

public sealed record EmployerResumeCertificationView(
    Guid Id,
    string Name,
    string? IssuingOrganization,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string? CertificateUrl,
    int SortOrder);

public sealed record EmployerResumePortfolioLinkView(
    Guid Id,
    string Title,
    string Url,
    string? Description,
    int SortOrder);
