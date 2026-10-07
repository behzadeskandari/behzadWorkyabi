using FluentValidation;
using IranJob.BuildingBlocks.Infrastructure.Configuration;
using IranJob.Modules.JobPostings.Application.Abstractions;
using IranJob.Modules.JobPostings.Application.Validators;
using IranJob.Modules.JobPostings.Infrastructure.Persistence;
using IranJob.Modules.JobPostings.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IranJob.Modules.JobPostings.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJobPostingsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
        var connectionString = configuration.GetConnectionString(databaseOptions.ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{databaseOptions.ConnectionStringName}' was not found.");

        services.AddDbContext<JobPostingDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "jobs");
            });
        });

        services.AddScoped<IJobPostingService, JobPostingService>();
        services.AddScoped<IJobApplicationService, JobApplicationService>();
        services.AddScoped<IPublicJobSearchService, PublicJobSearchService>();
        services.AddValidatorsFromAssemblyContaining<CreateJobPostingRequestValidator>();
        return services;
    }
}
