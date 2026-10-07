using FluentValidation;
using IranJob.BuildingBlocks.Infrastructure.Configuration;
using IranJob.Modules.ReferenceData.Application.Abstractions;
using IranJob.Modules.ReferenceData.Application.Validators;
using IranJob.Modules.ReferenceData.Infrastructure.Persistence;
using IranJob.Modules.ReferenceData.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IranJob.Modules.ReferenceData.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReferenceDataModule(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
        var connectionString = configuration.GetConnectionString(databaseOptions.ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{databaseOptions.ConnectionStringName}' was not found.");

        services.AddDbContext<ReferenceDataDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "reference_data");
            });
        });

        services.AddScoped<IReferenceDataService, ReferenceDataService>();
        services.AddValidatorsFromAssemblyContaining<NamedReferenceDataInputValidator>();
        return services;
    }
}
