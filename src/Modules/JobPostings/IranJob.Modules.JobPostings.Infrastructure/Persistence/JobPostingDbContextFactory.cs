using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace IranJob.Modules.JobPostings.Infrastructure.Persistence;

public sealed class JobPostingDbContextFactory : IDesignTimeDbContextFactory<JobPostingDbContext>
{
    public JobPostingDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost;Database=IranJob;Integrated Security=True;TrustServerCertificate=True";
        var optionsBuilder = new DbContextOptionsBuilder<JobPostingDbContext>();
        optionsBuilder.UseSqlServer(connectionString, sql =>
        {
            sql.MigrationsHistoryTable("__EFMigrationsHistory", "jobs");
        });

        return new JobPostingDbContext(optionsBuilder.Options);
    }
}
