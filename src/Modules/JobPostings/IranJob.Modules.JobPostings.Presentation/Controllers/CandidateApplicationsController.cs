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
[Route("api/v{version:apiVersion}/candidates/applications")]
[Authorize(Roles = IdentityRoles.Candidate)]
public sealed class CandidateApplicationsController(
    IJobApplicationService applicationService,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<JobApplicationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<JobApplicationResponse>>> GetMine(CancellationToken cancellationToken) =>
        Ok(await applicationService.GetOwnAsync(GetCurrentUserId(), cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(JobApplicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobApplicationResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await applicationService.GetOwnByIdAsync(GetCurrentUserId(), id, cancellationToken));

    [HttpPost("{id:guid}/withdraw")]
    [ProducesResponseType(typeof(JobApplicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<JobApplicationResponse>> Withdraw(Guid id, CancellationToken cancellationToken) =>
        Ok(await applicationService.WithdrawAsync(GetCurrentUserId(), id, cancellationToken));

    private Guid GetCurrentUserId() => currentUser.UserId ?? throw new UnauthorizedException();
}
