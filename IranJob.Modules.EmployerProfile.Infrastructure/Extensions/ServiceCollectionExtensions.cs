using FluentValidation;
using IranJob.BuildingBlocks.Infrastructure.Configuration;
using IranJob.Modules.EmployerProfile.Application.Abstractions;
using IranJob.Modules.EmployerProfile.Application.Validators;
using IranJob.Modules.EmployerProfile.Infrastructure.Persistence;
using IranJob.Modules.EmployerProfile.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IranJob.Modules.EmployerProfile.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEmployerProfileModule(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
            ?? new DatabaseOptions();

        var connectionString = configuration.GetConnectionString(databaseOptions.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{databaseOptions.ConnectionStringName}' was not found.");

        services.AddDbContext<EmployerDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "employers");
            });
        });

        services.AddScoped<IEmployerProfileService, EmployerProfileService>();
        services.AddValidatorsFromAssemblyContaining<EmployerProfileRequestValidator>();

        return services;
    }
}
