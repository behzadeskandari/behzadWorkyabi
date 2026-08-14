using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using IranJob.Api.Models;
using IranJob.Api.Services;

namespace IranJob.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system")]
public sealed class SystemController(ISystemInfoService systemInfoService) : ControllerBase
{
    [HttpGet("info")]
    [ProducesResponseType(typeof(SystemInfoResponse), StatusCodes.Status200OK)]
    public ActionResult<SystemInfoResponse> GetInfo()
    {
        return Ok(systemInfoService.GetSystemInfo());
    }
}
