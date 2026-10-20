using IranJob.Modules.Messages.Domain.Enums;
using IranJob.SharedKernel.Entities;

namespace IranJob.Modules.Messages.Domain.Entities;

/// <summary>
/// Joins an Identity user to a conversation. The <see cref="ProfileId"/> identifies
/// the belonging <c>CandidateProfile</c> or <c>EmployerProfile</c>, while
/// <see cref="IdentityUserId"/> is the immutable anchor used for authorisation
/// (a client can never supply its own identity; it is always resolved server-side
/// from the authenticated request).
/// </summary>
public sealed class ConversationParticipant : Entity
{
    private ConversationParticipant() { }

    public Guid ConversationId { get; private set; }

    public Guid ProfileId { get; private set; }

    public Guid IdentityUserId { get; private set; }

    public ParticipantRole Role { get; private set; }

    /// <summary>
    /// Inclusive timestamp up to which this participant has read. Null means
    /// the participant has read everything from the beginning.
    /// </summary>
    public DateTimeOffset? LastReadMessageAt { get; private set; }

    public Conversation Conversation { get; private set; } = null!;

    public static ConversationParticipant Create(Guid conversationId, Guid profileId, Guid identityUserId, ParticipantRole role)
    {
        return new ConversationParticipant
        {
            ConversationId = conversationId,
            ProfileId = profileId,
            IdentityUserId = identityUserId,
            Role = role,
            LastReadMessageAt = null
        };
    }

    /// <summary>
    /// Advances the read watermark to <paramref name="readUpTo"/>. Only moves
    /// forward: repeated or out-of-order calls are safe no-ops.
    /// </summary>
    public void MarkAsRead(DateTimeOffset readUpTo)
    {
        if (!LastReadMessageAt.HasValue || readUpTo > LastReadMessageAt.Value)
        {
            LastReadMessageAt = readUpTo;
        }
    }
}
