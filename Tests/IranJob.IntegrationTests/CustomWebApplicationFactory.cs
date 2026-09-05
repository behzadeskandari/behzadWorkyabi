using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using IranJob.BuildingBlocks.Infrastructure.Persistence;
using IranJob.Modules.Identity.Infrastructure.Persistence;
using IranJob.Modules.Identity.Infrastructure.Extensions;

namespace IranJob.IntegrationTests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

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

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            services.AddDbContext<IdentityDbContext>(options =>
            {
                options.UseSqlite(_connection);
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

        await applicationDbContext.Database.EnsureCreatedAsync();
        await identityDbContext.Database.EnsureCreatedAsync();
        
        await IdentitySeedData.SeedAsync(Services);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection?.Dispose();
        }

        base.Dispose(disposing);
    }
}
