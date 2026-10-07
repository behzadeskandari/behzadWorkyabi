using IranJob.Modules.JobPostings.Domain.Enums;
using IranJob.SharedKernel.Exceptions;

namespace IranJob.Modules.JobPostings.Domain.Entities;

public sealed class JobPosting
{
    private JobPosting() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid EmployerProfileId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid CategoryId { get; private set; }
    public EmploymentType EmploymentType { get; private set; }
    public WorkArrangement WorkArrangement { get; private set; }
    public Guid? CityId { get; private set; }
    public decimal? SalaryMinimum { get; private set; }
    public decimal? SalaryMaximum { get; private set; }
    public string? SalaryCurrency { get; private set; }
    public SalaryPeriod? SalaryPeriod { get; private set; }
    public JobPostingStatus Status { get; private set; } = JobPostingStatus.Draft;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public DateTimeOffset? ClosingAt { get; private set; }
    public ICollection<JobPostingSkill> Skills { get; private set; } = new List<JobPostingSkill>();

    public static JobPosting Create(Guid employerProfileId, JobPostingDetails details)
    {
        if (employerProfileId == Guid.Empty)
            throw new ArgumentException("Employer profile id is required.", nameof(employerProfileId));

        var posting = new JobPosting { EmployerProfileId = employerProfileId };
        posting.ApplyDetails(details);
        return posting;
    }

    public void Update(JobPostingDetails details)
    {
        if (Status == JobPostingStatus.Closed)
            throw new DomainException("A closed job posting cannot be edited.");

        ApplyDetails(details);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Publish()
    {
        if (Status != JobPostingStatus.Draft)
            throw new DomainException("Only a draft job posting can be published.");

        if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Description) || CategoryId == Guid.Empty)
            throw new DomainException("The job posting is incomplete and cannot be published.");

        if (CityId is null)
            throw new DomainException("A city is required for every job posting.");

        if (ClosingAt.HasValue && ClosingAt <= DateTimeOffset.UtcNow)
            throw new DomainException("The closing date must be in the future.");

        Status = JobPostingStatus.Published;
        PublishedAt = DateTimeOffset.UtcNow;
        UpdatedAt = PublishedAt;
    }

    public void Unpublish()
    {
        if (Status != JobPostingStatus.Published)
            throw new DomainException("Only a published job posting can be unpublished.");

        Status = JobPostingStatus.Draft;
        PublishedAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Close()
    {
        if (Status != JobPostingStatus.Published)
            throw new DomainException("Only a published job posting can be closed.");

        Status = JobPostingStatus.Closed;
        ClosedAt = DateTimeOffset.UtcNow;
        UpdatedAt = ClosedAt;
    }

    private void ApplyDetails(JobPostingDetails details)
    {
        Title = details.Title.Trim();
        Description = details.Description.Trim();
        CategoryId = details.CategoryId;
        EmploymentType = details.EmploymentType;
        WorkArrangement = details.WorkArrangement;
        CityId = details.CityId;
        SalaryMinimum = details.SalaryMinimum;
        SalaryMaximum = details.SalaryMaximum;
        SalaryCurrency = details.SalaryCurrency?.Trim().ToUpperInvariant();
        SalaryPeriod = details.SalaryPeriod;
        ClosingAt = details.ClosingAt;
        Skills = details.SkillIds.Distinct().Select(skillId => JobPostingSkill.Create(Id, skillId)).ToList();
    }
}
