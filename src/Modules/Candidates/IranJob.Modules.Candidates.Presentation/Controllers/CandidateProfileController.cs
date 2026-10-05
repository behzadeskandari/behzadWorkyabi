using Asp.Versioning;
using IranJob.BuildingBlocks.Infrastructure.CurrentService;
using IranJob.Modules.Candidates.Application.Abstractions;
using IranJob.Modules.Candidates.Presentation.Contracts;
using IranJob.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IranJob.Modules.Candidates.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/candidates/profile")]
[Authorize]
public sealed class CandidateProfileController(
    ICandidateProfileService profileService,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(CandidateProfileResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidateProfileResponseDto>> Get(CancellationToken cancellationToken)
    {
        var profile = await profileService.GetProfileAsync(GetCurrentUserId(), cancellationToken);
        return Ok(MapResponse(profile));
    }

    [HttpPost]
    [ProducesResponseType(typeof(CandidateProfileResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CandidateProfileResponseDto>> Create(
        [FromBody] SaveCandidateProfileRequestDto request,
        CancellationToken cancellationToken)
    {
        var profile = await profileService.CreateProfileAsync(GetCurrentUserId(), ToRequest(request), cancellationToken);
        return CreatedAtAction(nameof(Get), MapResponse(profile));
    }

    [HttpPut]
    [ProducesResponseType(typeof(CandidateProfileResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidateProfileResponseDto>> Update(
        [FromBody] SaveCandidateProfileRequestDto request,
        CancellationToken cancellationToken)
    {
        var profile = await profileService.UpdateProfileAsync(GetCurrentUserId(), ToRequest(request), cancellationToken);
        return Ok(MapResponse(profile));
    }

    private Guid GetCurrentUserId() =>
        currentUser.UserId ?? throw new UnauthorizedException();

    private static CandidateProfileRequest ToRequest(SaveCandidateProfileRequestDto request) =>
        new(
            request.Headline,
            request.Biography,
            request.DateOfBirth,
            request.Gender,
            request.City,
            request.Province,
            request.LinkedInUrl,
            request.GitHubUrl,
            request.PortfolioUrl,
            request.ExpectedSalary,
            request.SalaryType,
            request.EmploymentStatus,
            request.Availability,
            request.MilitaryStatus);

    private static CandidateProfileResponseDto MapResponse(CandidateProfileResult profile) =>
        new(
            profile.Id,
            profile.UserId,
            profile.Headline,
            profile.Biography,
            profile.DateOfBirth,
            profile.Gender,
            profile.City,
            profile.Province,
            profile.LinkedInUrl,
            profile.GitHubUrl,
            profile.PortfolioUrl,
            profile.ExpectedSalary,
            profile.SalaryType,
            profile.EmploymentStatus,
            profile.Availability,
            profile.MilitaryStatus,
            profile.CreatedAt,
            profile.UpdatedAt);
}
