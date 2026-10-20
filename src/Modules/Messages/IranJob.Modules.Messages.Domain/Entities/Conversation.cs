using IranJob.Modules.Messages.Domain.Enums;
using IranJob.SharedKernel.Entities;

namespace IranJob.Modules.Messages.Domain.Entities;

/// <summary>
/// A conversation is a persistent two-party (Candidate + Employer) thread that
/// tracks the job/application context it belongs to and the participants'
/// read state.
/// </summary>
public sealed class Conversation : Entity
{
    private Conversation() { }

    public string Subject { get; private set; } = string.Empty;

    public Guid? JobPostingId { get; private set; }

    public Guid? JobApplicationId { get; private set; }

    public DateTimeOffset? LastMessageAt { get; private set; }

    public ICollection<ConversationParticipant> Participants { get; } = new List<ConversationParticipant>();

    public ICollection<Message> Messages { get; } = new List<Message>();

    public static Conversation Create(string subject, Guid? jobPostingId, Guid? jobApplicationId)
    {
        var trimmed = subject?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new ArgumentException("Conversation subject cannot be empty.", nameof(subject));
        }

        if (trimmed.Length > 280)
        {
            throw new ArgumentException("Conversation subject cannot exceed 280 characters.", nameof(subject));
        }

        return new Conversation
        {
            Subject = trimmed,
            JobPostingId = jobPostingId,
            JobApplicationId = jobApplicationId,
            CreatedAt = DateTimeOffset.UtcNow.DateTime
        };
    }

    public ConversationParticipant? GetParticipantForUser(Guid identityUserId)
    {
        return Participants.FirstOrDefault(p => p.IdentityUserId == identityUserId);
    }

    /// <summary>
    /// Advances the conversation's last-activity watermark to the newest message.
    /// </summary>
    public void RecordActivity(DateTimeOffset sentAt)
    {
        if (!LastMessageAt.HasValue || sentAt > LastMessageAt.Value)
        {
            LastMessageAt = sentAt;
        }

        UpdatedAt = sentAt.UtcDateTime;
    }

    public void AddParticipant(ConversationParticipant participant)
    {
        if (participant is null)
        {
            throw new ArgumentNullException(nameof(participant));
        }

        if (Participants.Any(p => p.ProfileId == participant.ProfileId))
        {
            throw new InvalidOperationException("A participant with this profile is already in the conversation.");
        }

        Participants.Add(participant);
    }
}
