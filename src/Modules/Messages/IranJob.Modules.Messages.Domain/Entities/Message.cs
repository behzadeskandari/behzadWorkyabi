using IranJob.SharedKernel.Entities;

namespace IranJob.Modules.Messages.Domain.Entities;

/// <summary>
/// A single persisted message inside a <see cref="Conversation"/>. The sender is
/// always resolved server-side from the authenticated user; the client never
/// supplies its own sender identity.
/// </summary>
public sealed class Message : Entity
{
    private Message() { }

    public Guid ConversationId { get; private set; }

    public Conversation Conversation { get; private set; } = null!;

    public Guid SenderProfileId { get; private set; }

    public Guid SenderUserId { get; private set; }

    /// <summary>
    /// Public display name of the sender (candidate headline or employer company
    /// name). These are the non-sensitive profile fields that the messaging UI is
    /// allowed to render.
    /// </summary>
    public string SenderName { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    public DateTimeOffset SentAt { get; private set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ReadAt { get; internal set; }

    public static Message Create(Guid conversationId, Guid senderProfileId, Guid senderUserId, string senderName, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Message content cannot be empty.", nameof(content));
        }

        if (content.Length > 20000)
        {
            throw new ArgumentException("Message content cannot exceed 20,000 characters.", nameof(content));
        }

        return new Message
        {
            ConversationId = conversationId,
            SenderProfileId = senderProfileId,
            SenderUserId = senderUserId,
            SenderName = senderName.Trim(),
            Content = content.Trim(),
            SentAt = DateTimeOffset.UtcNow
        };
    }

    public void MarkAsRead()
    {
        ReadAt = DateTimeOffset.UtcNow;
    }
}
