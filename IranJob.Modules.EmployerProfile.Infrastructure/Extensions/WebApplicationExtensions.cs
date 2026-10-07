using IranJob.BuildingBlocks.Infrastructure.Configuration;
using IranJob.Modules.EmployerProfile.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IranJob.Modules.EmployerProfile.Infrastructure.Extensions;

public static class WebApplicationExtensions
{
    public static async Task ApplyEmployerProfileMigrationsAsync(this WebApplication app)
    {
        var databaseOptions = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;

        if (!databaseOptions.ApplyMigrationsOnStartup)
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EmployerDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<EmployerDbContext>>();

        try
        {
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Employer profile migrations applied successfully.");
        }
        catch (Exception exception) when (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
        {
            logger.LogWarning(
                exception,
                "Employer profile migrations could not be applied. Ensure SQL Server is running and the connection string is correct.");
        }
    }
}
