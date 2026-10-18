using IranJob.Modules.Notifications.Domain.Enums;

namespace IranJob.Modules.Notifications.Application.Contracts;

/// <summary>
/// A single notification item in list responses.
/// </summary>
public sealed record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    Guid? RelatedResourceId,
    DateTime CreatedAt,
    bool IsRead,
    DateTime? ReadAt);

/// <summary>
/// Paged list of notifications for the authenticated recipient, plus the
/// total unread count so a UI badge can render without an extra request.
/// </summary>
public sealed record NotificationListResponse(
    IReadOnlyList<NotificationDto> Items,
    int TotalCount,
    int UnreadCount,
    int Page,
    int PageSize);
