using IranJob.Modules.Candidates.Application.Contracts;

namespace IranJob.Modules.Candidates.Presentation.Contracts;

/// <summary>
/// DTO returned by the resume GET endpoint.
/// </summary>
public sealed record ResumeResponseDto(
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

