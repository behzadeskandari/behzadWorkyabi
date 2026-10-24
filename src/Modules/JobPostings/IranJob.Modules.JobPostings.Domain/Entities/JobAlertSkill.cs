namespace IranJob.Modules.JobPostings.Domain.Entities;

public sealed class JobAlertSkill
{
    private JobAlertSkill() { }
    public Guid JobAlertId { get; private set; }
    public Guid SkillId { get; private set; }
    public JobAlert JobAlert { get; private set; } = null!;
    internal static JobAlertSkill Create(Guid alertId, Guid skillId) => new() { JobAlertId = alertId, SkillId = skillId };
}
