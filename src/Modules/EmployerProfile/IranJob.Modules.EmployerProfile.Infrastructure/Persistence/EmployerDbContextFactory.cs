using IranJob.BuildingBlocks.Infrastructure.Configuration;
using IranJob.Modules.EmployerProfile.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace IranJob.Modules.EmployerProfile.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so <c>dotnet ef migrations add</c> can discover and
/// create the <c>employers</c>-schema database for this module.
/// </summary>
public sealed class EmployerDbContextFactory : IDesignTimeDbContextFactory<EmployerDbContext>
{
    public EmployerDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost,1433;Database=IranJob;User Id=sa;Password=Your_strong_Password123;TrustServerCertificate=True;Encrypt=False;";

        var optionsBuilder = new DbContextOptionsBuilder<EmployerDbContext>();
        optionsBuilder.UseSqlServer(connectionString, sql =>
        {
            sql.MigrationsHistoryTable("__EFMigrationsHistory", "employers");
        });

        return new EmployerDbContext(optionsBuilder.Options);
    }
}
