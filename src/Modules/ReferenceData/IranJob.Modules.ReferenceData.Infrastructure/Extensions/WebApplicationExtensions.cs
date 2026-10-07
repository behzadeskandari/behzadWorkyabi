using IranJob.BuildingBlocks.Infrastructure.Configuration;
using IranJob.Modules.ReferenceData.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IranJob.Modules.ReferenceData.Infrastructure.Extensions;

public static class WebApplicationExtensions
{
    public static async Task ApplyReferenceDataMigrationsAsync(this WebApplication app)
    {
        var databaseOptions = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        if (!databaseOptions.ApplyMigrationsOnStartup)
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReferenceDataDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ReferenceDataDbContext>>();
        try
        {
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Reference data migrations applied successfully.");
        }
        catch (Exception exception) when (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
        {
            logger.LogWarning(exception, "Reference data migrations could not be applied. Check the configured database connection.");
        }
    }
}
