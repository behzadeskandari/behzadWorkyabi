using IranJob.Modules.Identity.Domain.Constants;
using IranJob.Modules.Identity.Domain.Entities;
using IranJob.Modules.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IranJob.Modules.Identity.Infrastructure.Persistence;

public static class IdentitySeedData
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<IdentityDbContext>>();

        foreach (var roleName in IdentityRoles.All)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new ApplicationRole(roleName));
            if (result.Succeeded)
            {
                logger.LogInformation("Seeded role {RoleName}", roleName);
            }
            else
            {
                logger.LogWarning("Failed to seed role {RoleName}: {Errors}", roleName, string.Join(", ", result.Errors.Select(error => error.Description)));
            }
        }
    }
}
