namespace IranJob.Modules.JobPostings.Domain.Entities;

public sealed class SavedJob
{
    private SavedJob() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CandidateProfileId { get; private set; }
    public Guid JobPostingId { get; private set; }
    public DateTimeOffset SavedAt { get; private set; } = DateTimeOffset.UtcNow;

    public static SavedJob Create(Guid candidateProfileId, Guid jobPostingId)
    {
        if (candidateProfileId == Guid.Empty || jobPostingId == Guid.Empty)
            throw new ArgumentException("Candidate and job identifiers are required.");
        return new SavedJob { CandidateProfileId = candidateProfileId, JobPostingId = jobPostingId };
    }
}
