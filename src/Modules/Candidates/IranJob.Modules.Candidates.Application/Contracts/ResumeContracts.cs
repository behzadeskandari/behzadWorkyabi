namespace IranJob.Modules.Candidates.Application.Contracts;

/// <summary>
/// Read model returned by resume queries and write operations. Section/skill
/// collections reuse the shared DTO types so the Presentation layer can project
/// them straight into <see cref="IranJob.Modules.Candidates.Presentation.Contracts.ResumeResponseDto"/>.
/// </summary>
public sealed record ResumeResult(
    Guid Id,
    Guid UserId,
    string? ProfessionalSummary,
    string Status,
    int CompletenessScore,
    bool HasCvFile,
    long? CvFileSizeBytes,
    DateTimeOffset? CvUploadedAt,
    ResumeSectionDto[] WorkExperiences,
    ResumeSectionDto[] Educations,
    ResumeSkillDto[] Skills,
    ResumeLanguageDto[] Languages,
    ResumeProjectDto[] Projects,
    ResumeCertificationDto[] Certifications,
    ResumePortfolioLinkDto[] PortfolioLinks,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Full payload for create-or-replace of a candidate resume.
/// </summary>
public sealed record SaveResumeRequest(
    string? ProfessionalSummary,
    string? Status,
    ResumeWorkExperienceItem[]? WorkExperiences,
    ResumeEducationItem[]? Educations,
    ResumeSkillItem[]? Skills,
    ResumeLanguageItem[]? Languages,
    ResumeProjectItem[]? Projects,
    ResumeCertificationItem[]? Certifications,
    ResumePortfolioLinkItem[]? PortfolioLinks);

public sealed record ResumeWorkExperienceItem(
    Guid? Id,
    string Company,
    string JobTitle,
    string? Description,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool IsCurrent);

public sealed record ResumeEducationItem(
    Guid? Id,
    string Institution,
    string Degree,
    string? FieldOfStudy,
    string? Grade,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool IsCurrent);

public sealed record ResumeSkillItem(
    Guid? Id,
    Guid SkillId,
    string SkillName,
    string? ProficiencyLevel,
    int? YearsOfExperience);

public sealed record ResumeLanguageItem(
    Guid? Id,
    string LanguageName,
    string ProficiencyLevel);

public sealed record ResumeProjectItem(
    Guid? Id,
    string Title,
    string? Description,
    string? Url,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool IsCurrent);

public sealed record ResumeCertificationItem(
    Guid? Id,
    string Name,
    string? IssuingOrganization,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string? CertificateUrl);

public sealed record ResumePortfolioLinkItem(
    Guid? Id,
    string Title,
    string Url,
    string? Description);

/// <summary>
/// Shared presentation-agnostic DTOs for resume sections. These are returned by
/// <see cref="ResumeResult"/> and consumed by the Presentation layer's response model.
/// </summary>
public sealed record ResumeSectionDto(
    Guid Id,
    string PrimaryLabel,
    string? SecondaryLabel,
    string? TertiaryLabel,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool IsCurrent,
    int DurationMonths,
    int SortOrder);

public sealed record ResumeSkillDto(
    Guid Id,
    Guid SkillId,
    string SkillName,
    string? ProficiencyLevel,
    int? YearsOfExperience,
    int SortOrder);

public sealed record ResumeLanguageDto(
    Guid Id,
    string LanguageName,
    string ProficiencyLevel,
    int SortOrder);

public sealed record ResumeProjectDto(
    Guid Id,
    string Title,
    string? Description,
    string? Url,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool IsCurrent,
    int SortOrder);

public sealed record ResumeCertificationDto(
    Guid Id,
    string Name,
    string? IssuingOrganization,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string? CertificateUrl,
    int SortOrder);

public sealed record ResumePortfolioLinkDto(
    Guid Id,
    string Title,
    string Url,
    string? Description,
    int SortOrder);
