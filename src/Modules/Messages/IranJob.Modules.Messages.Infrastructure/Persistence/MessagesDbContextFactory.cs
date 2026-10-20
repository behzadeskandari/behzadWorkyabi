using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace IranJob.Modules.Messages.Infrastructure.Persistence;

public sealed class MessagesDbContextFactory : IDesignTimeDbContextFactory<MessagesDbContext>
{
    public MessagesDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost,1433;Database=IranJob;User Id=sa;Password=Your_strong_Password123;TrustServerCertificate=True;Encrypt=False";
        var optionsBuilder = new DbContextOptionsBuilder<MessagesDbContext>();
        optionsBuilder.UseSqlServer(connectionString, sql =>
        {
            sql.MigrationsHistoryTable("__EFMigrationsHistory", "messages");
        });

        return new MessagesDbContext(optionsBuilder.Options);
    }
}
