using IranJob.BuildingBlocks.Infrastructure.Configuration;
using IranJob.Modules.Candidates.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IranJob.Modules.Candidates.Infrastructure.Extensions;

public static class WebApplicationExtensions
{
    public static async Task ApplyCandidatesMigrationsAsync(this WebApplication app)
    {
        var databaseOptions = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;

        if (!databaseOptions.ApplyMigrationsOnStartup)
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CandidateDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<CandidateDbContext>>();

        try
        {
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Candidates migrations applied successfully.");
        }
        catch (Exception exception) when (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
        {
            logger.LogWarning(
                exception,
                "Candidates migrations could not be applied. Ensure SQL Server is running and the connection string is correct.");
        }
    }
}
