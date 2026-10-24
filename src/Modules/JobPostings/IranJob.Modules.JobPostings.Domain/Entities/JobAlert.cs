namespace IranJob.Modules.JobPostings.Domain.Entities;

public sealed class JobAlert
{
    private JobAlert() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CandidateProfileId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Guid? CategoryId { get; private set; }
    public Guid? CityId { get; private set; }
    public string? WorkArrangement { get; private set; }
    public bool IsEnabled { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public ICollection<JobAlertSkill> Skills { get; private set; } = new List<JobAlertSkill>();

    public static JobAlert Create(Guid candidateProfileId, string name, Guid? categoryId, Guid? cityId,
        string? workArrangement, IEnumerable<Guid> skillIds) =>
        new JobAlert { CandidateProfileId = candidateProfileId }.Update(name, categoryId, cityId, workArrangement, skillIds);

    public JobAlert Update(string name, Guid? categoryId, Guid? cityId, string? workArrangement, IEnumerable<Guid> skillIds)
    {
        var normalized = name?.Trim() ?? string.Empty;
        var skills = skillIds.Distinct().ToArray();
        if (normalized.Length is < 1 or > 100 || skills.Length > 30)
            throw new ArgumentException("Alert name or skill count is outside the supported range.");
        Name = normalized;
        CategoryId = categoryId;
        CityId = cityId;
        WorkArrangement = string.IsNullOrWhiteSpace(workArrangement) ? null : workArrangement;
        var selected = skills.ToHashSet();
        foreach (var oldSkill in Skills.Where(x => !selected.Contains(x.SkillId)).ToArray())
            Skills.Remove(oldSkill);
        foreach (var skillId in selected.Where(id => Skills.All(x => x.SkillId != id)))
            Skills.Add(JobAlertSkill.Create(Id, skillId));
        UpdatedAt = DateTimeOffset.UtcNow;
        return this;
    }

    public void SetEnabled(bool enabled) { IsEnabled = enabled; UpdatedAt = DateTimeOffset.UtcNow; }
}
