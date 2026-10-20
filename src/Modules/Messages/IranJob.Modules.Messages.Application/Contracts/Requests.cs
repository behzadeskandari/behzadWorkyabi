namespace IranJob.Modules.Messages.Application.Contracts;

/// <summary>
/// Request to start or retrieve a conversation. The two required identifiers
/// must reference a published job posting and (for candidates) an application by
/// that candidate to that job. The subject is optional; the API derives a sensible
/// default from the job title when omitted.
/// </summary>
public sealed record CreateConversationRequest(
    Guid JobPostingId,
    Guid? JobApplicationId,
    string? Subject);

/// <summary>
/// Request to send a message. Explicitly no <c>SenderId</c> or
/// <c>SenderProfileId</c> fields: the sender is always the authenticated user,
/// resolved server-side.
/// </summary>
public sealed record SendMessageRequest(
    Guid ConversationId,
    string Content);
