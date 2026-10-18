using IranJob.Modules.Notifications.Domain.Enums;
using IranJob.SharedKernel.Entities;

namespace IranJob.Modules.Notifications.Domain.Entities;

/// <summary>
/// A persistent, recipient-scoped in-app notification for an important
/// job-application lifecycle event. Recipient identity is always the
/// application user id (from Identity), resolved at persistence time by
/// the service layer — never trusted from the client.
/// </summary>
public sealed class Notification : Entity
{
    private Notification() { }

    public Guid RecipientUserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = default!;
    public string? LocalizedKey { get; private set; }
    public Guid? RelatedResourceId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }

    public static Notification Create(
        Guid recipientUserId,
        NotificationType type,
        string title,
        Guid? relatedResourceId = null)
    {
        if (recipientUserId == Guid.Empty)
            throw new ArgumentException("Recipient user id is required.", nameof(recipientUserId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        return new Notification
        {
            RecipientUserId = recipientUserId,
            Type = type,
            Title = title,
            RelatedResourceId = relatedResourceId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkRead()
    {
        if (IsRead)
            return;
        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }
}
