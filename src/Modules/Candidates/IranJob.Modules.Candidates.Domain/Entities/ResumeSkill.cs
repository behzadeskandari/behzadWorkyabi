using System;
using IranJob.SharedKernel.Entities;

namespace IranJob.Modules.Candidates.Domain.Entities;

/// <summary>
/// A skill linked from the ReferenceData skill catalogue, stored on the
/// candidate's resume. The skill name is denormalised so the resume can be
/// rendered even if the underlying reference-data skill is later deactivated.
/// </summary>
public sealed class ResumeSkill : Entity
{
    public Guid ResumeId { get; private set; }
    public Guid SkillId { get; private set; }
    public string SkillName { get; private set; } = string.Empty;
    public string? ProficiencyLevel { get; private set; }
    public int? YearsOfExperience { get; private set; }
    public int SortOrder { get; private set; }

    private ResumeSkill() { }

    public static ResumeSkill Create(
        Guid resumeId, Guid skillId, string skillName,
        string? proficiencyLevel, int? yearsOfExperience, int sortOrder)
    {
        if (skillId == Guid.Empty)
            throw new ArgumentException("SkillId cannot be empty.", nameof(skillId));

        return new ResumeSkill
        {
            Id = Guid.NewGuid(),
            ResumeId = resumeId,
            SkillId = skillId,
            SkillName = skillName.Trim(),
            ProficiencyLevel = NormalizeOptional(proficiencyLevel),
            YearsOfExperience = yearsOfExperience,
            SortOrder = sortOrder
        };
    }

    public void Update(string? proficiencyLevel, int? yearsOfExperience)
    {
        ProficiencyLevel = NormalizeOptional(proficiencyLevel);
        YearsOfExperience = yearsOfExperience;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}