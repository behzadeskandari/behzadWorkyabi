using IranJob.SharedKernel.Exceptions;

namespace IranJob.Modules.JobPostings.Domain.Entities;

public sealed class JobApplication
{
    private JobApplication() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CandidateProfileId { get; private set; }
    public Guid JobPostingId { get; private set; }
    public JobApplicationStatus Status { get; private set; } = JobApplicationStatus.Applied;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; private set; }

    public static JobApplication Create(Guid candidateProfileId, Guid jobPostingId)
    {
        if (candidateProfileId == Guid.Empty)
            throw new ArgumentException("Candidate profile id is required.", nameof(candidateProfileId));
        if (jobPostingId == Guid.Empty)
            throw new ArgumentException("Job posting id is required.", nameof(jobPostingId));

        return new JobApplication { CandidateProfileId = candidateProfileId, JobPostingId = jobPostingId };
    }

    public void Withdraw()
    {
        if (Status != JobApplicationStatus.Applied)
            throw new DomainException("Only an active application can be withdrawn.");

        Status = JobApplicationStatus.Withdrawn;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Review()
    {
        if (Status != JobApplicationStatus.Applied)
            throw new DomainException("Only an application with 'Applied' status can be reviewed.");

        Status = JobApplicationStatus.Reviewed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Accept()
    {
        if (Status != JobApplicationStatus.Applied && Status != JobApplicationStatus.Reviewed)
            throw new DomainException("Only an application with 'Applied' or 'Reviewed' status can be accepted.");

        Status = JobApplicationStatus.Accepted;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Reject()
    {
        if (Status != JobApplicationStatus.Applied && Status != JobApplicationStatus.Reviewed)
            throw new DomainException("Only an application with 'Applied' or 'Reviewed' status can be rejected.");

        Status = JobApplicationStatus.Rejected;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public enum JobApplicationStatus
{
    Applied,
    Withdrawn,
    Reviewed,
    Accepted,
    Rejected
}
