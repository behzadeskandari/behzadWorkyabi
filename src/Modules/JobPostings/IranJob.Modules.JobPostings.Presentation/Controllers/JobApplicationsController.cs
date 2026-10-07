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
[Route("api/v{version:apiVersion}/jobs/{jobId:guid}/applications")]
[Authorize(Roles = IdentityRoles.Candidate)]
public sealed class JobApplicationsController(
    IJobApplicationService applicationService,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(JobApplicationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<JobApplicationResponse>> Apply(Guid jobId, CancellationToken cancellationToken)
    {
        var application = await applicationService.ApplyAsync(GetCurrentUserId(), jobId, cancellationToken);
        return CreatedAtAction(
            nameof(CandidateApplicationsController.GetById),
            "CandidateApplications",
            new { version = "1", id = application.Id },
            application);
    }

    private Guid GetCurrentUserId() => currentUser.UserId ?? throw new UnauthorizedException();
}
