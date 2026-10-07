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
[Route("api/v{version:apiVersion}/employers/applications")]
[Authorize(Roles = IdentityRoles.Employer)]
public sealed class EmployerApplicationsController(
    IJobApplicationService applicationService,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EmployerApplicationListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<EmployerApplicationListResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await applicationService.GetEmployerApplicationsAsync(GetCurrentUserId(), cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EmployerApplicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployerApplicationResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await applicationService.GetEmployerApplicationByIdAsync(GetCurrentUserId(), id, cancellationToken));

    [HttpPost("{id:guid}/review")]
    [ProducesResponseType(typeof(EmployerApplicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployerApplicationResponse>> Review(Guid id, CancellationToken cancellationToken) =>
        Ok(await applicationService.ReviewApplicationAsync(GetCurrentUserId(), id, cancellationToken));

    [HttpPost("{id:guid}/accept")]
    [ProducesResponseType(typeof(EmployerApplicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployerApplicationResponse>> Accept(Guid id, CancellationToken cancellationToken) =>
        Ok(await applicationService.AcceptApplicationAsync(GetCurrentUserId(), id, cancellationToken));

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(EmployerApplicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployerApplicationResponse>> Reject(Guid id, CancellationToken cancellationToken) =>
        Ok(await applicationService.RejectApplicationAsync(GetCurrentUserId(), id, cancellationToken));

    private Guid GetCurrentUserId() => currentUser.UserId ?? throw new UnauthorizedException();
}