namespace IranJob.Modules.Candidates.Domain.Enums;

/// <summary>
/// Current workflow state of a candidate resume.
/// </summary>
public enum ResumeStatus
{
    /// <summary>
    /// The resume is a work-in-progress and not yet shareable.
    /// </summary>
    Draft = 1,

    /// <summary>
    /// All required sections are filled and the resume is marked complete.
    /// An optional CV document may or may not be present.
    /// </summary>
    Complete = 2,

    ///summary>
    /// The resume is complete and the candidate consents to sharing it
    /// with employers who have an authorized job application.
    /// </summary>
    ReadyToShare = 3
}