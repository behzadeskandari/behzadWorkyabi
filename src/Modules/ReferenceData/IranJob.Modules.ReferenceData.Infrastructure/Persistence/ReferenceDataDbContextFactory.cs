using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace IranJob.Modules.ReferenceData.Infrastructure.Persistence;

public sealed class ReferenceDataDbContextFactory : IDesignTimeDbContextFactory<ReferenceDataDbContext>
{
    public ReferenceDataDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost,1433;Database=IranJob;User Id=sa;Password=Your_strong_Password123;TrustServerCertificate=True;Encrypt=False";
        var optionsBuilder = new DbContextOptionsBuilder<ReferenceDataDbContext>();
        optionsBuilder.UseSqlServer(connectionString, sql =>
        {
            sql.MigrationsHistoryTable("__EFMigrationsHistory", "reference_data");
        });

        return new ReferenceDataDbContext(optionsBuilder.Options);
    }
}
