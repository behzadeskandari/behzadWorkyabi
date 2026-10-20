using System;
using System.Linq;

namespace IranJob.Modules.Messages.Application.Contracts;

public sealed class ConversationListItem
{
    public Guid Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public Guid? JobPostingId { get; set; }
    public string? JobTitle { get; set; }
    public string? LastMessagePreview { get; set; }
    public DateTimeOffset? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
    public int ParticipantCount { get; set; }
}

public sealed class UnreadItem
{
    public Guid ConversationId { get; set; }
    public int UnreadCount { get; set; }
}

public sealed class UnreadSummary
{
    public int TotalUnread { get; set; }
    public IReadOnlyList<UnreadItem> Items { get; set; } = Array.Empty<UnreadItem>();
}

public sealed class MessageDto
{
    public MessageDto(Guid id, Guid conversationId, Guid senderProfileId, string senderName, string content, DateTimeOffset sentAt, bool isRead)
    {
        Id = id;
        ConversationId = conversationId;
        SenderProfileId = senderProfileId;
        SenderName = senderName;
        Content = content;
        SentAt = sentAt;
        IsRead = isRead;
    }

    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderProfileId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset SentAt { get; set; }
    public bool IsRead { get; set; }
}

