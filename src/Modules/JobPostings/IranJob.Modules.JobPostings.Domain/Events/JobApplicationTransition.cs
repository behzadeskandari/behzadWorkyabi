namespace IranJob.Modules.JobPostings.Domain.Events;

/// <summary>
/// The discrete status transitions of a <see cref="JobApplication"/> for
/// which a notification is raised.
/// </summary>
public enum JobApplicationTransition
{
    /// <summary>Candidate submitted a new application (recipient: employer).</summary>
    Applied = 1,

    /// <summary>Candidate withdrew an active application (recipient: employer).</summary>
    Withdrawn = 2,

    /// <summary>Employer marked the application as reviewed (recipient: candidate).</summary>
    Reviewed = 3,

    /// <summary>Employer accepted the application (recipient: candidate).</summary>
    Accepted = 4,

    /// <summary>Employer rejected the application (recipient: candidate).</summary>
    Rejected = 5
}
