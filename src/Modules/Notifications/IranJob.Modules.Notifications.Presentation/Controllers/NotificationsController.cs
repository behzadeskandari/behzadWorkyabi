using Asp.Versioning;
using IranJob.BuildingBlocks.Infrastructure.CurrentService;
using IranJob.Modules.Identity.Domain.Constants;
using IranJob.Modules.Notifications.Application.Abstractions;
using IranJob.Modules.Notifications.Application.Contracts;
using IranJob.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IranJob.Modules.Notifications.Presentation.Controllers;

/// <summary>
/// Authenticated, recipient-scoped notification inbox. The recipient id is
/// always derived from the authenticated principal via
/// <see cref="ICurrentUserService"/> — never from the request body or route.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications")]
[Authorize(Roles = IdentityRoles.Candidate + "," + IdentityRoles.Employer + "," + IdentityRoles.Recruiter + "," + IdentityRoles.Admin + "," + IdentityRoles.SuperAdmin)]

public sealed class NotificationsController(
    INotificationService notificationService,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(NotificationListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<NotificationListResponse>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await notificationService.ListAsync(GetCurrentUserId(), page, pageSize, cancellationToken));

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<int>> GetUnreadCount(CancellationToken cancellationToken) =>
        Ok(await notificationService.GetUnreadCountAsync(GetCurrentUserId(), cancellationToken));

    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        var marked = await notificationService.MarkReadAsync(GetCurrentUserId(), id, cancellationToken);
        if (!marked)
            throw new NotFoundException("Notification was not found.");
        return Ok(marked);
    }

    [HttpPatch("mark-all-read")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<int>> MarkAllRead(CancellationToken cancellationToken) =>
        Ok(await notificationService.MarkAllReadAsync(GetCurrentUserId(), cancellationToken));

    private Guid GetCurrentUserId() => currentUser.UserId ?? throw new UnauthorizedException();
}
