using Asp.Versioning;
using IranJob.BuildingBlocks.Infrastructure.CurrentService;
using IranJob.Modules.Candidates.Application.Abstractions;
using IranJob.Modules.Candidates.Application.Contracts;
using IranJob.Modules.Candidates.Presentation.Contracts;

using IranJob.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IranJob.Modules.Candidates.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/candidates/resume")]
[Authorize]
public sealed class CandidateResumeController(
    ICandidateResumeService resumeService,
    ICurrentUserService currentUser) : ControllerBase
{
    private Guid GetCurrentUserId() => currentUser.UserId ?? throw new UnauthorizedException();

    [HttpGet]
    [ProducesResponseType(typeof(ResumeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResumeResponseDto>> GetResume(CancellationToken cancellationToken)
    {
        var result = await resumeService.GetResumeAsync(GetCurrentUserId(), cancellationToken);
        return Ok(MapToDto(result));
    }

    [HttpPut]
    [ProducesResponseType(typeof(ResumeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ResumeResponseDto>> UpsertResume(
        SaveResumeRequest request,
        CancellationToken cancellationToken)
    {
        var skillReferences = request.Skills is null
            ? Enumerable.Empty<SkillReference>()
            : request.Skills.Select(s => new SkillReference(s.SkillId, s.SkillName));

        var result = await resumeService.UpsertResumeAsync(
            GetCurrentUserId(), request, skillReferences, cancellationToken);
        return Ok(MapToDto(result));
    }

    [HttpPost("cv")]
    [DisableRequestSizeLimit]
    [ProducesResponseType(typeof(ResumeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ResumeResponseDto>> UploadCv(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var result = await resumeService.UploadCvAsync(
            GetCurrentUserId(),
            file.OpenReadStream(),
            file.FileName,
            file.ContentType,
            file.Length,
            cancellationToken);
        return Ok(MapToDto(result));
    }

    [HttpGet("cv")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadCv(CancellationToken cancellationToken)
    {
        var download = await resumeService.DownloadCvAsync(GetCurrentUserId(), cancellationToken);
        return File(download.Content, download.ContentType, download.DownloadName);
    }

    [HttpDelete("cv")]
    [ProducesResponseType(typeof(ResumeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResumeResponseDto>> DeleteCv(CancellationToken cancellationToken)
    {
        var result = await resumeService.DeleteCvAsync(GetCurrentUserId(), cancellationToken);
        return Ok(MapToDto(result));
    }

    [HttpPost("status")]
    [ProducesResponseType(typeof(ResumeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResumeResponseDto>> SetStatus(
        [FromQuery] string status,
        CancellationToken cancellationToken)
    {
        var result = await resumeService.SetStatusAsync(GetCurrentUserId(), status, cancellationToken);
        return Ok(MapToDto(result));
    }

    private static ResumeResponseDto MapToDto(ResumeResult result) => new(
        result.Id, result.UserId, result.ProfessionalSummary, result.Status,
        result.CompletenessScore, result.HasCvFile, result.CvFileSizeBytes, result.CvUploadedAt,
        result.WorkExperiences.Select(MapSection).ToArray(),
        result.Educations.Select(MapSection).ToArray(),
        result.Skills.Select(s => new ResumeSkillDto(
            s.Id, s.SkillId, s.SkillName, s.ProficiencyLevel, s.YearsOfExperience, s.SortOrder)).ToArray(),
        result.Languages.Select(l => new ResumeLanguageDto(
            l.Id, l.LanguageName, l.ProficiencyLevel, l.SortOrder)).ToArray(),
        result.Projects.Select(p => new ResumeProjectDto(
            p.Id, p.Title, p.Description, p.Url, p.StartDate, p.EndDate, p.IsCurrent, p.SortOrder)).ToArray(),
        result.Certifications.Select(c => new ResumeCertificationDto(
            c.Id, c.Name, c.IssuingOrganization, c.IssueDate, c.ExpiryDate, c.CertificateUrl, c.SortOrder)).ToArray(),
        result.PortfolioLinks.Select(p => new ResumePortfolioLinkDto(
            p.Id, p.Title, p.Url, p.Description, p.SortOrder)).ToArray(),
        result.CreatedAt, result.UpdatedAt);

    private static ResumeSectionDto MapSection(ResumeSectionDto source) => new(
        source.Id, source.PrimaryLabel, source.SecondaryLabel, source.TertiaryLabel,
        source.StartDate, source.EndDate, source.IsCurrent, source.DurationMonths, source.SortOrder);
}
