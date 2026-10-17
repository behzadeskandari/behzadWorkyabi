using Asp.Versioning;
using IranJob.BuildingBlocks.Infrastructure.CurrentService;
using IranJob.Modules.Identity.Domain.Constants;
using IranJob.Modules.JobPostings.Application.Abstractions;
using IranJob.Modules.JobPostings.Application.Contracts;
using IranJob.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IranJob.Modules.JobPostings.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/jobs")]
[Authorize(Policy = PolicyNames.AdminOnly)]
public sealed class AdminJobsController(
    IAdminJobModerationService moderationService,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(AdminJobPageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminJobPageResult>> GetJobs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? cityId = null,
        [FromQuery] string? employerName = null,
        [FromQuery] string sort = "Newest",
        CancellationToken cancellationToken = default) =>
        Ok(await moderationService.GetJobsAsync(page, pageSize, search, status, categoryId, cityId, employerName, sort, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminJobDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminJobDetailResponse>> GetJobById(Guid id, CancellationToken cancellationToken) =>
        Ok(await moderationService.GetJobByIdAsync(id, cancellationToken));

    [HttpPost("{id:guid}/unpublish")]
    [ProducesResponseType(typeof(AdminJobDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminJobDetailResponse>> ModerateUnpublish(
        Guid id,
        [FromBody] ModerateJobRequest request,
        CancellationToken cancellationToken)
    {
        var result = await moderationService.ModerateUnpublishAsync(id, RequireActorUserId(), request.Reason, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(typeof(AdminJobDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminJobDetailResponse>> ModeratePublish(
        Guid id,
        [FromBody] ModerateJobRequest request,
        CancellationToken cancellationToken)
    {
        var result = await moderationService.ModeratePublishAsync(id, RequireActorUserId(), request.Reason, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(typeof(AdminJobDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminJobDetailResponse>> ModerateClose(
        Guid id,
        [FromBody] ModerateJobRequest request,
        CancellationToken cancellationToken)
    {
        var result = await moderationService.ModerateCloseAsync(id, RequireActorUserId(), request.Reason, cancellationToken);
        return Ok(result);
    }

    private Guid RequireActorUserId()
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedException("Authenticated user ID is required.");

        return userId;
    }
}