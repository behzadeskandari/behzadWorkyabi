using IranJob.BuildingBlocks.Infrastructure.Logging;
using IranJob.Modules.Identity.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace IranJob.Modules.Identity.Infrastructure.Services;

public sealed class RequestContext(IHttpContextAccessor httpContextAccessor) : IRequestContext
{
    public string? IpAddress =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent =>
        httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();

    public string? CorrelationId =>
        httpContextAccessor.HttpContext?.Items[CorrelationIdConstants.ItemKey]?.ToString()
        ?? httpContextAccessor.HttpContext?.Request.Headers[CorrelationIdConstants.HeaderName].ToString();
}
