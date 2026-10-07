namespace IranJob.Modules.JobPostings.Domain.Entities;

public sealed class JobPostingSkill
{
    private JobPostingSkill() { }

    public Guid JobPostingId { get; private set; }
    public Guid SkillId { get; private set; }
    public JobPosting JobPosting { get; private set; } = null!;

    internal static JobPostingSkill Create(Guid jobPostingId, Guid skillId) => new()
    {
        JobPostingId = jobPostingId,
        SkillId = skillId
    };
}
