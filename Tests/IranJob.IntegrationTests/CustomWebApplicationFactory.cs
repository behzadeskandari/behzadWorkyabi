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
            using IranJob.Modules.Payments.Application.Abstractions;
            using IranJob.Modules.Payments.Infrastructure.Configuration;
            using IranJob.Modules.Payments.Infrastructure.Persistence;
            using IranJob.Modules.Payments.Infrastructure.Services;
            using IranJob.IntegrationTests.TestStubs;

namespace IranJob.IntegrationTests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Shared-cache in-memory databases. Each DbContext opens its own connection (thread-safe)
    // to the same in-memory cache; the keep-alive connections below keep the databases alive
    // for the lifetime of the test server. Default Timeout=30 sets a 30s busy_timeout so
    // concurrent writers wait instead of surfacing SQLITE_BUSY / NullReference.
    private const string InMemoryDatabaseProvider = "Data Source=InMemory;Mode=Memory;Cache=Shared;Default Timeout=30";
    private const string InMemoryIdentityProvider = "Data Source=InMemoryIdentity;Mode=Memory;Cache=Shared;Default Timeout=30";
    private SqliteConnection? _keepAliveConnection;
    private SqliteConnection? _identityKeepAliveConnection;
    private readonly SemaphoreSlim _jobDatabaseLock = new(1, 1);
    private bool _jobDatabaseInitialized;

    /// <summary>
    /// Publish fee charged to employers for publishing a job posting.
    /// Default is 0 (free) so existing job-lifecycle tests publish immediately.
    /// Set to a non-zero value in payment tests to exercise the gateway flow.
    /// </summary>
    public long PublishFeeAmount { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Open keep-alive connections first so the shared-cache in-memory databases
        // exist before any scoped DbContext attaches to them.
        _keepAliveConnection = new SqliteConnection(InMemoryDatabaseProvider);
        _keepAliveConnection.Open();

        _identityKeepAliveConnection = new SqliteConnection(InMemoryIdentityProvider);
        _identityKeepAliveConnection.Open();

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
            services.RemoveAll<DbContextOptions<PaymentDbContext>>();
            services.RemoveAll<PaymentDbContext>();

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(InMemoryIdentityProvider);
            });

            services.AddDbContext<IdentityDbContext>(options =>
            {
                options.UseSqlite(InMemoryIdentityProvider);
            });
            services.AddDbContext<CandidateDbContext>(options =>
            {
                options.UseSqlite(InMemoryDatabaseProvider);
            });
            services.AddDbContext<EmployerDbContext>(options =>
            {
                options.UseSqlite(InMemoryDatabaseProvider);
            });
            services.AddDbContext<ReferenceDataDbContext>(options =>
            {
                options.UseSqlite(InMemoryDatabaseProvider);
            });
            services.AddDbContext<JobPostingDbContext>(options =>
            {
                options.UseSqlite(InMemoryDatabaseProvider);
            });

            // Payments module: in-memory SQLite, fee-free by default so existing job-lifecycle tests publish immediately. Payment tests set PublishFeeAmount to exercise the fee-gated gateway flow; each PaymentDbContext opens its own thread-safe connection to the shared job database (Cache=Shared + per-DbContext connections eliminate the shared-SqliteConnection race).
            services.AddDbContext<PaymentDbContext>(options =>
            {
                options.UseSqlite(InMemoryDatabaseProvider);
            });
            services.Configure<PaymentOptions>(options => { options.PublishFeeAmount = PublishFeeAmount; });
            services.AddSingleton<IZarinPalClient, StubZarinPalClient>();
            services.AddScoped<IPaymentService, PaymentService>();


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
    var paymentDbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

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
            await paymentDbContext.Database.ExecuteSqlRawAsync(paymentDbContext.Database.GenerateCreateScript());
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
            _keepAliveConnection?.Dispose();
            _identityKeepAliveConnection?.Dispose();
        }

        base.Dispose(disposing);
    }
}












