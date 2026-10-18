using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IranJob.Modules.Notifications.Application.Abstractions;
using IranJob.Modules.Notifications.Application.Contracts;
using IranJob.Modules.Notifications.Domain.Entities;
using IranJob.Modules.Notifications.Domain.Enums;
using IranJob.Modules.Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IranJob.Modules.Notifications.Infrastructure.Services;

/// <summary>
/// Reads and writes notifications scoped to a single authenticated recipient
/// user id. The recipient id is always supplied by the caller (the event
/// handler / controller, which derives it from the authenticated identity),
/// never from a client request body — so cross-user access is structurally
/// impossible at this layer.
/// </summary>
public sealed class NotificationService(
    NotificationDbContext dbContext,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task<NotificationListResponse> ListAsync(
        Guid recipientUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (recipientUserId == Guid.Empty)
            throw new UnauthorizedAccessException("Recipient user id is required.");

        await ValidateBoundsAsync(page, pageSize);

        var baseQuery = dbContext.Notifications
            .AsNoTracking()
            .Where(notification => notification.RecipientUserId == recipientUserId);

        var total = await baseQuery.CountAsync(cancellationToken);
        var unread = await baseQuery.CountAsync(notification => !notification.IsRead, cancellationToken);

        var items = await baseQuery
            .OrderByDescending(notification => notification.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(notification => Map(notification))
            .ToListAsync(cancellationToken);

        return new NotificationListResponse(items, total, unread, page, pageSize);
    }

    public async Task<int> GetUnreadCountAsync(Guid recipientUserId, CancellationToken cancellationToken = default)
    {
        if (recipientUserId == Guid.Empty)
            throw new UnauthorizedAccessException("Recipient user id is required.");

        return await dbContext.Notifications
            .AsNoTracking()
            .Where(notification => notification.RecipientUserId == recipientUserId && !notification.IsRead)
            .CountAsync(cancellationToken);
    }

    public async Task<bool> MarkReadAsync(Guid recipientUserId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        if (recipientUserId == Guid.Empty)
            throw new UnauthorizedAccessException("Recipient user id is required.");

        // Scope the update to this recipient: a different user's notification is a
        // no-op (caller observes "not found"), preventing cross-user mutation.
        var affected = await dbContext.Notifications
            .Where(notification => notification.RecipientUserId == recipientUserId
                                   && notification.Id == notificationId
                                   && !notification.IsRead)
            .ExecuteUpdateAsync(
                set => set
                    .SetProperty(notification => notification.IsRead, true)
                    .SetProperty(notification => notification.ReadAt, DateTime.UtcNow),
                cancellationToken);

        return affected > 0;
    }

    public async Task<int> MarkAllReadAsync(Guid recipientUserId, CancellationToken cancellationToken = default)
    {
        if (recipientUserId == Guid.Empty)
            throw new UnauthorizedAccessException("Recipient user id is required.");

        return await dbContext.Notifications
            .Where(notification => notification.RecipientUserId == recipientUserId && !notification.IsRead)
            .ExecuteUpdateAsync(
                set => set
                    .SetProperty(notification => notification.IsRead, true)
                    .SetProperty(notification => notification.ReadAt, DateTime.UtcNow),
                cancellationToken);
    }

    public async Task<Guid> CreateIfNotExistsAsync(
        Guid recipientUserId,
        NotificationType type,
        string title,
        Guid? relatedResourceId = null,
        CancellationToken cancellationToken = default)
    {
        if (recipientUserId == Guid.Empty)
            throw new UnauthorizedAccessException("Recipient user id is required.");

        // Idempotency: dedupe by (recipient, type, related resource). A retry or
        // repeated dispatch therefore never produces a duplicate notification.
        var existing = await dbContext.Notifications
            .Where(notification => notification.RecipientUserId == recipientUserId
                                   && notification.Type == type
                                   && notification.RelatedResourceId == relatedResourceId)
            .Select(notification => notification.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing != Guid.Empty)
        {
            logger.LogDebug(
                "Notification of type {Type} for resource {Resource} to recipient {Recipient} already exists (id {Id}); skipping insert.",
                type, relatedResourceId, recipientUserId, existing);
            return existing;
        }

        var notification = Notification.Create(recipientUserId, type, title, relatedResourceId);
        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);
        return notification.Id;
    }

    private static async Task ValidateBoundsAsync(int page, int pageSize)
    {
        if (page < 1)
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be a positive 1-based index.");
        if (pageSize is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 100.");
    }

    private static NotificationDto Map(Notification notification) => new(
        notification.Id,
        notification.Type,
        notification.Title,
        notification.RelatedResourceId,
        notification.CreatedAt,
        notification.IsRead,
        notification.ReadAt);
}
