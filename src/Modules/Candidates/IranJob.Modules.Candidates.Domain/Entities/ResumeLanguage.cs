using System;
using IranJob.SharedKernel.Entities;

namespace IranJob.Modules.Candidates.Domain.Entities;

/// <summary>
/// A language proficiency entry on the candidate's resume.
/// </summary>
public sealed class ResumeLanguage : Entity
{
    public Guid ResumeId { get; private set; }
    public string LanguageName { get; private set; } = string.Empty;
    public string ProficiencyLevel { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }

    private ResumeLanguage() { }

    public static ResumeLanguage Create(
        Guid resumeId, string languageName, string proficiencyLevel, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(languageName))
            throw new ArgumentException("Language name is required.", nameof(languageName));
        if (string.IsNullOrWhiteSpace(proficiencyLevel))
            throw new ArgumentException("Proficiency level is required.", nameof(proficiencyLevel));

        return new ResumeLanguage
        {
            Id = Guid.NewGuid(),
            ResumeId = resumeId,
            LanguageName = languageName.Trim(),
            ProficiencyLevel = proficiencyLevel.Trim(),
            SortOrder = sortOrder
        };
    }

    public void Update(string languageName, string proficiencyLevel)
    {
        LanguageName = languageName.Trim();
        ProficiencyLevel = proficiencyLevel.Trim();
    }
}