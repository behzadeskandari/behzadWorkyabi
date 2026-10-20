using FluentValidation;
using IranJob.BuildingBlocks.Infrastructure.Configuration;
using IranJob.Modules.Notifications.Application.Abstractions;
using IranJob.Modules.Notifications.Infrastructure.Persistence;
using IranJob.Modules.Notifications.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IranJob.Modules.Notifications.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
                              ?? new DatabaseOptions();
        var connectionString = configuration.GetConnectionString(databaseOptions.ConnectionStringName)
                               ?? throw new InvalidOperationException($"Connection string '{databaseOptions.ConnectionStringName}' was not found.");

        services.AddDbContext<NotificationDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "notifications");
            });
        });

        services.AddScoped<INotificationService, NotificationService>();
        services.AddValidatorsFromAssemblyContaining<NotificationService>();

        // Outbound email. Bound from the "Email" section; disabled by default so
        // environments without an SMTP server (tests, local dev) no-op cleanly.
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddScoped<IEmailService, SmtpEmailService>();

        return services;
    }
}
