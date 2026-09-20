using FluentValidation;
using IranJob.BuildingBlocks.Infrastructure.Configuration;
using IranJob.Modules.Candidates.Application.Abstractions;
using IranJob.Modules.Candidates.Infrastructure.Persistence;
using IranJob.Modules.Candidates.Infrastructure.Services;
using IranJob.Modules.Candidates.Application.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IranJob.Modules.Candidates.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCandidatesModule(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
        var connectionString = configuration.GetConnectionString(databaseOptions.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{databaseOptions.ConnectionStringName}' was not found.");

        services.AddDbContext<CandidateDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "candidates");
            });
        });

        services.AddScoped<ICandidateProfileService, CandidateProfileService>();

        services.AddValidatorsFromAssemblyContaining<CandidateProfileRequestValidator>();

        return services;
    }
}
