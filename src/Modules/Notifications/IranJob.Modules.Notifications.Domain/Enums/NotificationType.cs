namespace IranJob.Modules.Notifications.Domain.Enums;

/// <summary>
/// The lifecycle event that produced a notification. Used both for routing
/// (which recipient, which UI wording) and as part of the idempotency key
/// when deduplicating notifications from retries or repeated dispatches.
/// </summary>
public enum NotificationType
{
    /// <summary>The employer has a new job application to review.</summary>
    ApplicationSubmitted = 1,

    /// <summary>The candidate withdrew an application.</summary>
    ApplicationWithdrawn = 2,

    /// <summary>The employer marked an application as reviewed.</summary>
    ApplicationReviewed = 3,

    /// <summary>The employer accepted an application.</summary>
    ApplicationAccepted = 4,

    /// <summary>The employer rejected an application.</summary>
    ApplicationRejected = 5
}
