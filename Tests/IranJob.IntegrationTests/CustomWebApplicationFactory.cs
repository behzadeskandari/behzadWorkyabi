using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using IranJob.BuildingBlocks.Infrastructure.Persistence;
using IranJob.Modules.Candidates.Infrastructure.Persistence;
using IranJob.Modules.Identity.Infrastructure.Persistence;
using IranJob.Modules.Identity.Infrastructure.Extensions;
using IranJob.Modules.ReferenceData.Infrastructure.Persistence;
using IranJob.Modules.EmployerProfile.Infrastructure.Persistence;
using IranJob.Modules.JobPostings.Infrastructure.Persistence;

namespace IranJob.IntegrationTests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;
    private SqliteConnection? _jobConnection;
    private readonly SemaphoreSlim _jobDatabaseLock = new(1, 1);
    private bool _jobDatabaseInitialized;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Candidate profiles, employers, reference data and jobs share the same
        // test database just as their schemas share the development SQL Server.
        _jobConnection = new SqliteConnection("DataSource=:memory:");
        _jobConnection.Open();

        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Application:Name"] = "IranJob",
                ["Application:Version"] = "0.1.0",
                ["Database:ApplyMigrationsOnStartup"] = "false",
                ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:",
                ["Authentication:Jwt:SecretKey"] = "TEST_SECRET_KEY_FOR_INTEGRATION_TESTS_MUST_BE_LONG_ENOUGH",
                ["Authentication:Jwt:Issuer"] = "TestIssuer",
                ["Authentication:Jwt:Audience"] = "TestAudience",
                ["Authentication:Identity:MaxFailedAccessAttempts"] = "5",
                ["Authentication:Identity:LockoutMinutes"] = "15",
                ["Authentication:Identity:RefreshTokenExpirationDays"] = "7",
                ["Authentication:Identity:RefreshTokenCookieName"] = "iranjob_refresh_token",
                ["Authentication:Identity:CsrfCookieName"] = "iranjob_csrf",
                ["Authentication:Identity:CsrfHeaderName"] = "X-CSRF-TOKEN",
                ["Authentication:RateLimiting:PermitLimit"] = "1000",
                ["Authentication:RateLimiting:WindowSeconds"] = "60"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions<IdentityDbContext>>();
            services.RemoveAll<IdentityDbContext>();
            services.RemoveAll<DbContextOptions<CandidateDbContext>>();
            services.RemoveAll<CandidateDbContext>();
            services.RemoveAll<DbContextOptions<EmployerDbContext>>();
            services.RemoveAll<EmployerDbContext>();
            services.RemoveAll<DbContextOptions<ReferenceDataDbContext>>();
            services.RemoveAll<ReferenceDataDbContext>();
            services.RemoveAll<DbContextOptions<JobPostingDbContext>>();
            services.RemoveAll<JobPostingDbContext>();

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            services.AddDbContext<IdentityDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });
            services.AddDbContext<CandidateDbContext>(options =>
            {
                options.UseSqlite(_jobConnection);
            });
            services.AddDbContext<EmployerDbContext>(options =>
            {
                options.UseSqlite(_jobConnection);
            });
            services.AddDbContext<ReferenceDataDbContext>(options =>
            {
                options.UseSqlite(_jobConnection);
            });
            services.AddDbContext<JobPostingDbContext>(options =>
            {
                options.UseSqlite(_jobConnection);
            });

            services.PostConfigure<HealthCheckServiceOptions>(options =>
            {
                options.Registrations.Clear();
            });
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var applicationDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var identityDbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var candidateDbContext = scope.ServiceProvider.GetRequiredService<CandidateDbContext>();
        var jobPostingDbContext = scope.ServiceProvider.GetRequiredService<JobPostingDbContext>();
        var employerDbContext = scope.ServiceProvider.GetRequiredService<EmployerDbContext>();
        var referenceDataDbContext = scope.ServiceProvider.GetRequiredService<ReferenceDataDbContext>();

        await applicationDbContext.Database.EnsureCreatedAsync();
        await identityDbContext.Database.EnsureCreatedAsync();
        await _jobDatabaseLock.WaitAsync();
        try
        {
            if (!_jobDatabaseInitialized)
            {
                await jobPostingDbContext.Database.EnsureCreatedAsync();
                await candidateDbContext.Database.ExecuteSqlRawAsync(candidateDbContext.Database.GenerateCreateScript());
                await referenceDataDbContext.Database.ExecuteSqlRawAsync(referenceDataDbContext.Database.GenerateCreateScript());
                await employerDbContext.Database.ExecuteSqlRawAsync(employerDbContext.Database.GenerateCreateScript());
                _jobDatabaseInitialized = true;
            }
        }
        finally
        {
            _jobDatabaseLock.Release();
        }

        await IdentitySeedData.SeedAsync(Services);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection?.Dispose();
            _jobConnection?.Dispose();
        }

        base.Dispose(disposing);
    }
}



