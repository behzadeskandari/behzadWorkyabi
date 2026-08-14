using Microsoft.Extensions.Options;
using IranJob.Api.Models;
using IranJob.BuildingBlocks.Infrastructure.Configuration;

namespace IranJob.Api.Services;

public interface ISystemInfoService
{
    SystemInfoResponse GetSystemInfo();
}

public sealed class SystemInfoService(
    IOptions<ApplicationOptions> applicationOptions,
    IWebHostEnvironment environment) : ISystemInfoService
{
    public SystemInfoResponse GetSystemInfo()
    {
        var options = applicationOptions.Value;

        return new SystemInfoResponse(
            options.Name,
            options.Version,
            environment.EnvironmentName);
    }
}
