using Asp.Versioning;
using IranJob.Modules.JobPostings.Application.Abstractions;
using IranJob.Modules.JobPostings.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IranJob.Modules.JobPostings.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/jobs")]
[AllowAnonymous]
public sealed class PublicJobsController(IPublicJobSearchService searchService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PublicJobSearchPage), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PublicJobSearchPage>> Search(
        [FromQuery] PublicJobSearchQuery query,
        CancellationToken cancellationToken) =>
        Ok(await searchService.SearchAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PublicJobDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicJobDetails>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await searchService.GetPublishedByIdAsync(id, cancellationToken));
}
