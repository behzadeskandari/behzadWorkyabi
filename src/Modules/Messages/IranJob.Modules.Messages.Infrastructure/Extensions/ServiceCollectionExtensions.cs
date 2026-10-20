using FluentValidation;
using IranJob.BuildingBlocks.Infrastructure.Configuration;
using IranJob.Modules.Messages.Application.Abstractions;
using IranJob.Modules.Messages.Application.Validators;
using IranJob.Modules.Messages.Infrastructure.Persistence;
using IranJob.Modules.Messages.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IranJob.Modules.Messages.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMessagesModule(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
                              ?? new DatabaseOptions();
        var connectionString = configuration.GetConnectionString(databaseOptions.ConnectionStringName)
                               ?? throw new InvalidOperationException($"Connection string '{databaseOptions.ConnectionStringName}' was not found.");

        services.AddDbContext<MessagesDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "messages");
            });
        });

        services.AddScoped<IConversationService, ConversationService>();
        services.AddValidatorsFromAssemblyContaining<CreateConversationRequestValidator>();

        return services;
    }
}
