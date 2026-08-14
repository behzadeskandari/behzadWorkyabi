using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using IranJob.SharedKernel.Exceptions;

namespace IranJob.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/diagnostics")]
public sealed class DiagnosticsController : ControllerBase
{
    [HttpGet("throw")]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult Throw()
    {
        throw new InvalidOperationException("Diagnostic exception for testing.");
    }

    [HttpGet("not-found")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult NotFoundSample()
    {
        throw new NotFoundException("The requested diagnostic resource was not found.");
    }
}
