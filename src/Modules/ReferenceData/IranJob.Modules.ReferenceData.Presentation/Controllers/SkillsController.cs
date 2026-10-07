using Asp.Versioning;
using IranJob.Modules.Identity.Domain.Constants;
using IranJob.Modules.ReferenceData.Application.Abstractions;
using IranJob.Modules.ReferenceData.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IranJob.Modules.ReferenceData.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/skills")]
public sealed class SkillsController(IReferenceDataService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ReferenceDataItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReferenceDataItem>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await service.GetSkillsAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReferenceDataItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReferenceDataItem>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetSkillAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Roles = $"{IdentityRoles.Admin},{IdentityRoles.SuperAdmin}")]
    [ProducesResponseType(typeof(ReferenceDataItem), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReferenceDataItem>> Create(NamedReferenceDataInput input, CancellationToken cancellationToken)
    {
        var created = await service.CreateSkillAsync(input, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{IdentityRoles.Admin},{IdentityRoles.SuperAdmin}")]
    [ProducesResponseType(typeof(ReferenceDataItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReferenceDataItem>> Update(Guid id, NamedReferenceDataInput input, CancellationToken cancellationToken) =>
        Ok(await service.UpdateSkillAsync(id, input, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{IdentityRoles.Admin},{IdentityRoles.SuperAdmin}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await service.DeactivateSkillAsync(id, cancellationToken);
        return NoContent();
    }
}
