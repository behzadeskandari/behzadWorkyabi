using IranJob.Modules.Messages.Application.Contracts;

namespace IranJob.Modules.Messages.Application.Abstractions;

/// <summary>
/// Application contract for candidate/employer direct messaging. Every operation
/// is authorised against the authenticated user's Identity identity.
/// </summary>
public interface IConversationService
{
    /// <summary>
    /// Starts (or retrieves) a conversation for the given job/application context.
    /// Only the job owner (employer) or the job's applicant (candidate) may start
    /// a conversation for that context.
    /// </summary>
    Task<ConversationListItem?> CreateConversationAsync(CreateConversationRequest request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a conversation, or null when the authenticated user is not a
    /// participant. Unauthorized access to another user's conversation is not
    /// possible through this API.
    /// </summary>
    Task<ConversationListItem?> GetConversationAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the authenticated user's conversations, oldest relevance first,
    /// paginated. Unread counts are reduced to the current user.
    /// </summary>
    Task<IReadOnlyList<ConversationListItem>> ListConversationsAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns paginated messages for a conversation. The caller must be a
    /// participant. Unread state is computed against the caller's read timestamp.
    /// </summary>
    Task<IReadOnlyList<MessageDto>> GetMessagesAsync(
        Guid conversationId, Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message. Only the sender is authorised; the recipient is derived
    /// from the conversation participants. Email is sent as a side effect after
    /// the message is committed.
    /// </summary>
    Task<MessageDto> SendMessageAsync(SendMessageRequest request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks all messages the current user has not yet read, up to the newest
    /// message in the conversation, as read. Fails if the user is not a participant.
    /// </summary>
    Task MarkAsReadAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns unread counts per conversation for the current user.
    /// </summary>
    Task<UnreadSummary> GetUnreadSummaryAsync(Guid userId, CancellationToken cancellationToken = default);
}
