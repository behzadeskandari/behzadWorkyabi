using Asp.Versioning;
using IranJob.BuildingBlocks.Infrastructure.CurrentService;
using IranJob.Modules.Identity.Domain.Constants;
using IranJob.Modules.JobPostings.Application.Contracts;
using IranJob.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IranJob.Modules.JobPostings.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/candidates")]
[Authorize(Roles = IdentityRoles.Candidate)]
public sealed class CandidateJobPreferencesController(
    ICandidateJobPreferenceService preferences,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpPut("saved-jobs/{jobId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SaveJob(Guid jobId, CancellationToken ct)
    {
        await preferences.SaveJobAsync(UserId, jobId, ct);
        return NoContent();
    }

    [HttpDelete("saved-jobs/{jobId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveSavedJob(Guid jobId, CancellationToken ct)
    {
        await preferences.RemoveSavedJobAsync(UserId, jobId, ct);
        return NoContent();
    }

    [HttpGet("saved-jobs")]
    [ProducesResponseType(typeof(SavedJobPage), StatusCodes.Status200OK)]
    public async Task<ActionResult<SavedJobPage>> ListSavedJobs([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await preferences.ListSavedJobsAsync(UserId, page, pageSize, ct));

    [HttpGet("saved-jobs/status")]
    [ProducesResponseType(typeof(IReadOnlyList<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Guid>>> SavedStatuses([FromQuery] List<Guid>? jobIds, CancellationToken ct) =>
        Ok(await preferences.GetSavedStatusesAsync(UserId, jobIds ?? [], ct));

    [HttpGet("job-alerts")]
    [ProducesResponseType(typeof(IReadOnlyList<JobAlertDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<JobAlertDto>>> ListAlerts(CancellationToken ct) =>
        Ok(await preferences.ListAlertsAsync(UserId, ct));

    [HttpPost("job-alerts")]
    [ProducesResponseType(typeof(JobAlertDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<JobAlertDto>> CreateAlert(JobAlertRequest request, CancellationToken ct)
    {
        var alert = await preferences.CreateAlertAsync(UserId, request, ct);
        return CreatedAtAction(nameof(ListAlerts), alert);
    }

    [HttpPut("job-alerts/{id:guid}")]
    [ProducesResponseType(typeof(JobAlertDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<JobAlertDto>> UpdateAlert(Guid id, JobAlertRequest request, CancellationToken ct) =>
        Ok(await preferences.UpdateAlertAsync(UserId, id, request, ct));

    [HttpDelete("job-alerts/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAlert(Guid id, CancellationToken ct)
    {
        await preferences.DeleteAlertAsync(UserId, id, ct);
        return NoContent();
    }

    private Guid UserId => currentUser.UserId ?? throw new UnauthorizedException();
}
