using Asp.Versioning;
using IranJob.Modules.ReferenceData.Application.Abstractions;
using IranJob.Modules.ReferenceData.Application.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IranJob.Modules.ReferenceData.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/salary-currencies")]
public sealed class SalaryCurrenciesController(IReferenceDataService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SalaryCurrencyItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SalaryCurrencyItem>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await service.GetSalaryCurrenciesAsync(cancellationToken));
}