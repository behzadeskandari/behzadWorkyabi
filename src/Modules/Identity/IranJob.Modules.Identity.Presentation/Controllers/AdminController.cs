using Asp.Versioning;
using IranJob.BuildingBlocks.Infrastructure.CurrentService;
using IranJob.Modules.Identity.Application.Abstractions;
using IranJob.Modules.Identity.Domain.Constants;
using IranJob.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IranJob.Modules.Identity.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin")]
[Authorize(Policy = PolicyNames.AdminOnly)]
public sealed class AdminController(
    IAdminDashboardService dashboardService,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet("ping")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult Ping() => Ok(new { message = "Admin access granted." });

    [HttpGet("stats")]
    [ProducesResponseType(typeof(SystemStatsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SystemStatsResult>> GetStats(CancellationToken cancellationToken) =>
        Ok(await dashboardService.GetSystemStatsAsync(cancellationToken));

    [HttpGet("users")]
    [ProducesResponseType(typeof(UserListResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserListResult>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default) =>
        Ok(await dashboardService.GetUsersAsync(page, pageSize, search, role, isActive, cancellationToken));

    [HttpGet("users/{userId:guid}")]
    [ProducesResponseType(typeof(UserDetailResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDetailResult>> GetUserById(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        Ok(await dashboardService.GetUserByIdAsync(userId, RequireActorUserId(), cancellationToken));

    [HttpPost("users/{userId:guid}/active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SetUserActiveStatus(
        Guid userId,
        [FromBody] SetUserActiveStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        await dashboardService.SetUserActiveStatusAsync(userId, request.IsActive, RequireActorUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("users/{userId:guid}/lockout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SetUserLockout(
        Guid userId,
        [FromBody] SetUserLockoutRequest request,
        CancellationToken cancellationToken = default)
    {
        await dashboardService.SetUserLockoutAsync(userId, request.LockOut, RequireActorUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("users/{userId:guid}/roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateUserRoles(
        Guid userId,
        [FromBody] UpdateUserRolesRequest request,
        CancellationToken cancellationToken = default)
    {
        await dashboardService.UpdateUserRolesAsync(userId, request.Roles, RequireActorUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("users/{userId:guid}/password-reset")]
    [ProducesResponseType(typeof(PasswordResetInitiationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PasswordResetInitiationResult>> InitiatePasswordReset(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        Ok(await dashboardService.InitiatePasswordResetAsync(userId, RequireActorUserId(), cancellationToken));

    [HttpGet("super-admin/can-manage-users")]
    [Authorize(Policy = PolicyNames.SuperAdminOnly)]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult SuperAdminCanManageUsers()
    {
        var userId = currentUser.UserId;
        return Ok(new { canManage = true, userId = userId?.ToString() ?? string.Empty });
    }

    private Guid RequireActorUserId()
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedException("Authenticated user ID is required.");

        return userId;
    }
}

