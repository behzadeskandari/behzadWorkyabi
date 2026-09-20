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

namespace IranJob.IntegrationTests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
        private SqliteConnection? _connection;
    private SqliteConnection? _candidateConnection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // The candidate profile is an isolated module; it stores only a UserId
        // reference and has no foreign keys to the Identity user table. In the
        // test host it gets its own in-memory SQLite database so that
        // EnsureCreated can build its schema independently and without clashing
        // with the migration-history table created by the other module DbContext
        // instances that share the connection above.
        _candidateConnection = new SqliteConnection("DataSource=:memory:");
        _candidateConnection.Open();

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
                options.UseSqlite(_candidateConnection);
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

        await applicationDbContext.Database.EnsureCreatedAsync();
        await identityDbContext.Database.EnsureCreatedAsync();
        await candidateDbContext.Database.EnsureCreatedAsync();
        
        await IdentitySeedData.SeedAsync(Services);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
                        _connection?.Dispose();
            _candidateConnection?.Dispose();
        }

        base.Dispose(disposing);
    }
}



