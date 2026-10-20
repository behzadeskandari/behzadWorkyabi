namespace IranJob.Modules.Messages.Domain.Enums;

/// <summary>
/// Fixed participant orientation for a direct conversation. A conversation is
/// always between exactly one Candidate and one Employer, so a participant is
/// classified by role rather than by a free-form direction.
/// </summary>
public enum ParticipantRole
{
    Candidate = 0,
    Employer = 1
}
