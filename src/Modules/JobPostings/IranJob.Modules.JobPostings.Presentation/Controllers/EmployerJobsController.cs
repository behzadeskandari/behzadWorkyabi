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
[Route("api/v{version:apiVersion}/employers/jobs")]
[Authorize(Roles = IdentityRoles.Employer)]
public sealed class EmployerJobsController(IJobPostingService service, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<JobPostingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<JobPostingResponse>>> GetOwn(CancellationToken cancellationToken) =>
        Ok(await service.GetOwnAsync(GetCurrentUserId(), cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(JobPostingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobPostingResponse>> GetOwnById(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetOwnByIdAsync(GetCurrentUserId(), id, cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(JobPostingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobPostingResponse>> Create(
        [FromBody] CreateJobPostingRequest request,
        CancellationToken cancellationToken)
    {
        var created = await service.CreateAsync(GetCurrentUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(GetOwnById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(JobPostingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<JobPostingResponse>> Update(
        Guid id,
        [FromBody] UpdateJobPostingRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.UpdateAsync(GetCurrentUserId(), id, request, cancellationToken));

    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(typeof(JobPostingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<JobPostingResponse>> Publish(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.PublishAsync(GetCurrentUserId(), id, cancellationToken));

    [HttpPost("{id:guid}/unpublish")]
    [ProducesResponseType(typeof(JobPostingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<JobPostingResponse>> Unpublish(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.UnpublishAsync(GetCurrentUserId(), id, cancellationToken));

    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(typeof(JobPostingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<JobPostingResponse>> Close(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.CloseAsync(GetCurrentUserId(), id, cancellationToken));

    private Guid GetCurrentUserId() => currentUser.UserId ?? throw new UnauthorizedException();
}
