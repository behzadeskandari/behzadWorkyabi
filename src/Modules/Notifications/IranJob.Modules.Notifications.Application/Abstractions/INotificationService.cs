using IranJob.Modules.Notifications.Application.Contracts;
using IranJob.Modules.Notifications.Domain.Enums;

namespace IranJob.Modules.Notifications.Application.Abstractions;

/// <summary>
/// Persists and queries notifications for an authenticated recipient.
/// Recipient identity is always supplied by the caller (derived from the
/// authenticated principal / <see cref="ICurrentUserService"/>); it is never
/// trusted from an end-client request.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Returns at most <paramref name="pageSize"/> unread notifications for
    /// the recipient, newest first. Used for a compact header dropdown.
    /// </summary>
    Task<NotificationListResponse> ListAsync(
        Guid recipientUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(Guid recipientUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a single notification as read, but only if it belongs to the
    /// supplied recipient (cross-user access returns NotFound).
    /// </summary>
    Task<bool> MarkReadAsync(Guid recipientUserId, Guid notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks every notification for the recipient as read. Returns the count
    /// of notifications that transitioned to read.
    /// </summary>
    Task<int> MarkAllReadAsync(Guid recipientUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a notification for <paramref name="recipientUserId"/> if an
    /// equivalent event (same type + related resource) does not already exist
    /// for that recipient, preventing duplicates from retries/dispatch fan-out.
    /// </summary>
    Task<Guid> CreateIfNotExistsAsync(
        Guid recipientUserId,
        NotificationType type,
        string title,
        string message,
        Guid? relatedResourceId = null,
        CancellationToken cancellationToken = default);
}
