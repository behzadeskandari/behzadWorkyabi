using IranJob.Modules.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IranJob.BuildingBlocks.Infrastructure.Configuration;

namespace IranJob.Modules.Identity.Infrastructure.Extensions;

public static class WebApplicationExtensions
{
    public static async Task ApplyIdentityMigrationsAsync(this WebApplication app)
    {
        var databaseOptions = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;

        if (!databaseOptions.ApplyMigrationsOnStartup)
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<IdentityDbContext>>();

        try
        {
            await dbContext.Database.MigrateAsync();
            await IdentitySeedData.SeedAsync(app.Services);
            logger.LogInformation("Identity migrations and seed data applied successfully.");
        }
        catch (Exception exception) when (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
        {
            logger.LogWarning(
                exception,
                "Identity migrations could not be applied. Ensure SQL Server is running and the connection string is correct.");
        }
    }

    public static WebApplication UseIdentityModule(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        return app;
    }
}
