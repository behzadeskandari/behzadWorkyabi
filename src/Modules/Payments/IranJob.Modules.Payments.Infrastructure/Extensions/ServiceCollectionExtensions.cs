using FluentValidation;
using IranJob.BuildingBlocks.Infrastructure.Configuration;
using IranJob.Modules.Payments.Application.Abstractions;
using IranJob.Modules.Payments.Application.Contracts;
using IranJob.Modules.Payments.Application.Validators;
using IranJob.Modules.Payments.Infrastructure.Configuration;
using IranJob.Modules.Payments.Infrastructure.Integrations.ZarinPal;
using IranJob.Modules.Payments.Infrastructure.Persistence;
using IranJob.Modules.Payments.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IranJob.Modules.Payments.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPaymentsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PaymentOptions>(configuration.GetSection(PaymentOptions.SectionName));

        var databaseOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
            ?? new DatabaseOptions();
        var connectionString = configuration.GetConnectionString(databaseOptions.ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{databaseOptions.ConnectionStringName}' was not found.");

        services.AddDbContext<PaymentDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "payments");
            });
        });

        services.AddHttpClient<IZarinPalClient, ZarinPalClient>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddValidatorsFromAssemblyContaining<CreatePaymentRequestValidator>();

        return services;
    }
}
