using Asp.Versioning;
using IranJob.BuildingBlocks.Infrastructure.CurrentService;
using IranJob.Modules.Candidates.Application.Abstractions;
using IranJob.Modules.Candidates.Application.Contracts;
using IranJob.Modules.Candidates.Presentation.Contracts;
using IranJob.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace IranJob.Modules.Candidates.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/candidates/profile")]
[Authorize]
public sealed class CandidateProfileController(
    ICandidateProfileService profileService,
    ICandidateResumeService resumeService,
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

    [HttpGet("completion")]
    [ProducesResponseType(typeof(CandidateProfileCompletionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CandidateProfileCompletionResponse>> GetCompletion(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var profileCompletion = await profileService.GetCompletionStatusAsync(userId, cancellationToken);
        var resumeCompletion = await resumeService.GetCompletionStatusAsync(userId, cancellationToken);

        var readiness = BuildReadiness(profileCompletion, resumeCompletion);
        var profileScore = profileCompletion?.CompletenessScore ?? 0;
        var response = new CandidateProfileCompletionResponse(
            ProfileCompletenessScore: profileScore,
            HasProfile: profileCompletion?.HasProfile ?? false,
            MissingProfileFields: profileCompletion?.MissingFields ?? Array.Empty<string>(),
            Resume: resumeCompletion,
            Readiness: readiness);

        return Ok(response);
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

    private static ApplicationReadiness BuildReadiness(
        ProfileCompletionResponse? profile,
        ResumeCompletionResponse? resume)
    {
        var hasRequiredProfile = profile?.HasProfile == true && string.IsNullOrEmpty(profile.MissingFields.FirstOrDefault());
        var hasProfile = profile?.HasProfile == true;
        var hasRecommendedResume = resume?.IsShared == true;

        var requiredMissing = new List<string>();
        if (!hasProfile) requiredMissing.Add("Headline");
        else requiredMissing.AddRange(profile.MissingFields.Where(f => f == "Headline"));

        var recommendedMissing = new List<string>();
        if (resume is null)
        {
            recommendedMissing.Add("Resume");
        }
        else if (!resume.IsShared)
        {
            recommendedMissing.Add("Resume.Shared");
        }

        var message = BuildReadinessMessage(hasRequiredProfile, hasRecommendedResume, resume?.IsShared == true && !hasRequiredProfile);
        var isReady = hasRequiredProfile;

        return new ApplicationReadiness(
            IsReady: isReady,
            HasRequiredProfile: hasRequiredProfile,
            HasRecommendedResume: hasRecommendedResume,
            RequiredMissing: requiredMissing.ToArray(),
            RecommendedMissing: recommendedMissing.ToArray(),
            Message: message);
    }

    private static string BuildReadinessMessage(bool hasRequiredProfile, bool hasRecommendedResume, bool resumeReadyButProfileMissing)
    {
        if (resumeReadyButProfileMissing)
        {
            return "رزومه‌تان آماده است، اما برای درخواست شغلی نیاز به تکمیل پروفایل شخصی دارید.";
        }
        if (!hasRequiredProfile)
        {
            return "برای ارسال درخواست شغلی ابتدا پروفایل حرفه‌ای خود را تکمیل کنید.";
        }
        if (!hasRecommendedResume)
        {
            return "می‌توانید درخواست بدهید، اما برای نمایش رزومه به کارفرمایان آن را تکمیل و منتشر کنید.";
        }
        return "آماده ارسال درخواست شغلی هستید.";
    }

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
