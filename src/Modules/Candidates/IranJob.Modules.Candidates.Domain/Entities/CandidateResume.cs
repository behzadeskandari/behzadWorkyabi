using System;
using System.Collections.Generic;
using System.Linq;
using IranJob.Modules.Candidates.Domain.Enums;
using IranJob.SharedKernel.Entities;

namespace IranJob.Modules.Candidates.Domain.Entities;

/// <summary>
/// Aggregate root representing a candidate's structured resume.
/// One resume exists per candidate (UserId). The resume references
/// existing profile data where appropriate; it does not duplicate
/// identity fields such as email or phone number.
/// </summary>
public sealed class CandidateResume : Entity
{
    public Guid UserId { get; private set; }
    public string? ProfessionalSummary { get; private set; }
    public ResumeStatus Status { get; private set; } = ResumeStatus.Draft;
    public int CompletenessScore { get; private set; }

    // CV document metadata. The actual file is stored on disk outside the
    // publicly-served web root and is only served through authorized endpoints.
    public string? CvFileName { get; private set; }
    public string? CvFileContentType { get; private set; }
    public long? CvFileSizeBytes { get; private set; }
    public DateTimeOffset? CvUploadedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private readonly List<ResumeWorkExperience> _workExperiences = new();
    private readonly List<ResumeEducation> _educations = new();
    private readonly List<ResumeSkill> _skills = new();
    private readonly List<ResumeLanguage> _languages = new();
    private readonly List<ResumeProject> _projects = new();
    private readonly List<ResumeCertification> _certifications = new();
    private readonly List<ResumePortfolioLink> _portfolioLinks = new();

    public IReadOnlyCollection<ResumeWorkExperience> WorkExperiences => _workExperiences.AsReadOnly();
    public IReadOnlyCollection<ResumeEducation> Educations => _educations.AsReadOnly();
    public IReadOnlyCollection<ResumeSkill> Skills => _skills.AsReadOnly();
    public IReadOnlyCollection<ResumeLanguage> Languages => _languages.AsReadOnly();
    public IReadOnlyCollection<ResumeProject> Projects => _projects.AsReadOnly();
    public IReadOnlyCollection<ResumeCertification> Certifications => _certifications.AsReadOnly();
    public IReadOnlyCollection<ResumePortfolioLink> PortfolioLinks => _portfolioLinks.AsReadOnly();

    private CandidateResume() { }

    public static CandidateResume Create(Guid userId, string? professionalSummary = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        return new CandidateResume
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProfessionalSummary = TrimToNull(professionalSummary)
        };
    }

    public void UpdateCore(string? professionalSummary)
    {
        ProfessionalSummary = TrimToNull(professionalSummary);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetCvFile(string fileName, string contentType, long fileSizeBytes)
    {
        CvFileName = fileName;
        CvFileContentType = contentType;
        CvFileSizeBytes = fileSizeBytes;
        CvUploadedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ClearCvFile()
    {
        CvFileName = null;
        CvFileContentType = null;
        CvFileSizeBytes = null;
        CvUploadedAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetStatus(ResumeStatus status)
    {
        Status = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public int CalculateCompleteness()
    {
        var score = 0;
        if (!string.IsNullOrWhiteSpace(ProfessionalSummary)) score += 15;
        if (_workExperiences.Count > 0) score += 15;
        if (_educations.Count > 0) score += 15;
        if (_skills.Count > 0) score += 15;
        if (_languages.Count > 0) score += 10;
        if (_projects.Count > 0) score += 10;
        if (_certifications.Count > 0) score += 10;
        if (_portfolioLinks.Count > 0) score += 5;
        if (CvFileName is not null) score += 5;
        return score;
    }

    private static string? TrimToNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}