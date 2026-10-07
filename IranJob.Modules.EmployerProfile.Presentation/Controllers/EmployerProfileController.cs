using Asp.Versioning;
using IranJob.BuildingBlocks.Infrastructure.CurrentService;
using IranJob.Modules.EmployerProfile.Application.Abstractions;
using IranJob.Modules.EmployerProfile.Presentation.Contracts;
using IranJob.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IranJob.Modules.EmployerProfile.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/employers/profile")]
[Authorize]
public sealed class EmployerProfileController(
    IEmployerProfileService profileService,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(EmployerProfileResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployerProfileResponseDto>> Get(CancellationToken cancellationToken)
    {
        var profile = await profileService.GetProfileAsync(GetCurrentUserId(), cancellationToken);
        return Ok(MapResponse(profile));
    }

    [HttpPost]
    [ProducesResponseType(typeof(EmployerProfileResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployerProfileResponseDto>> Create(
        [FromBody] SaveEmployerProfileRequestDto request,
        CancellationToken cancellationToken)
    {
        var profile = await profileService.CreateProfileAsync(GetCurrentUserId(), ToRequest(request), cancellationToken);
        return CreatedAtAction(nameof(Get), MapResponse(profile));
    }

    [HttpPut]
    [ProducesResponseType(typeof(EmployerProfileResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployerProfileResponseDto>> Update(
        [FromBody] SaveEmployerProfileRequestDto request,
        CancellationToken cancellationToken)
    {
        var profile = await profileService.UpdateProfileAsync(GetCurrentUserId(), ToRequest(request), cancellationToken);
        return Ok(MapResponse(profile));
    }

    [HttpPost("logo")]
    [ProducesResponseType(typeof(EmployerProfileResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployerProfileResponseDto>> UploadLogo(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { file = new[] { "لوگوی شرکت الزامی است." } });

        await using var stream = file.OpenReadStream();

        var profile = await profileService.UploadLogoAsync(
            GetCurrentUserId(),
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            cancellationToken);

        return Ok(MapResponse(profile));
    }

    private Guid GetCurrentUserId() =>
        currentUser.UserId ?? throw new UnauthorizedException();

    private static EmployerProfileRequest ToRequest(SaveEmployerProfileRequestDto request) =>
        new(
            request.CompanyName,
            request.Industry,
            request.CompanySize,
            request.City,
            request.Province,
            request.CompanyDescription,
            request.WebsiteUrl,
            request.LinkedInUrl,
            request.LogoUrl,
            request.FoundedYear,
            request.Address,
            request.PostalCode,
            request.ContactEmail,
            request.ContactPhone);

    private static EmployerProfileResponseDto MapResponse(EmployerProfileResult profile) =>
        new(
            profile.Id,
            profile.UserId,
            profile.CompanyName,
            profile.CompanyDescription,
            profile.Industry,
            profile.CompanySize,
            profile.WebsiteUrl,
            profile.LinkedInUrl,
            profile.LogoUrl,
            profile.FoundedYear,
            profile.City,
            profile.Province,
            profile.Address,
            profile.PostalCode,
            profile.ContactEmail,
            profile.ContactPhone,
            profile.CreatedAt,
            profile.UpdatedAt);
}