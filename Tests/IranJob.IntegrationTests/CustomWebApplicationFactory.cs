using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
using IranJob.Modules.Notifications.Infrastructure.Persistence;
using IranJob.Modules.Notifications.Application.Abstractions;
using IranJob.Modules.Messages.Infrastructure.Persistence;
using IranJob.IntegrationTests.TestStubs;

namespace IranJob.IntegrationTests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Shared-cache in-memory databases. Each DbContext opens its own connection (thread-safe)
    // to the same in-memory cache; the keep-alive connections below keep the databases alive
    // for the lifetime of the test server. Default Timeout=30 sets a 30s busy_timeout so
    // concurrent writers wait instead of surfacing SQLITE_BUSY / NullReference.
    // The data source name is unique PER factory instance. xUnit constructs one
    // CustomWebApplicationFactory per test class (PublicJobSearchTests creates its own
    // `new()`), so a unique name isolates each class's in-memory database and stops
    // parallel classes from racing on a single shared schema.
    private readonly string InMemoryDatabaseProvider = $"Data Source=InMemory-{Guid.NewGuid():N};Mode=Memory;Cache=Shared;Default Timeout=30";
    private readonly string InMemoryIdentityProvider = $"Data Source=InMemoryIdentity-{Guid.NewGuid():N};Mode=Memory;Cache=Shared;Default Timeout=30";
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

    /// <summary>
    /// Overrides the default factory behavior.
    ///
    /// The base implementation calls HostFactoryResolver to find a static
    /// <c>CreateHostBuilder</c> method on the entry-point assembly (Program).
    /// With .NET 8 minimal-hosting top-level statements, that method does NOT
    /// exist, so HostFactoryResolver returns null and the factory falls into
    /// the DeferredHostBuilder path. In that path, the factory's
    /// <c>ConfigureWebHost</c> callbacks (stored on the IHostBuilder
    /// Properties) are NOT replayed onto the host that Program.Main creates,
    /// meaning <see cref="ConfigureWebHost"/> here is never invoked — the
    /// SQLite substitutions in ConfigureWebHost never run, environment stays
    /// at the default "Production", and SQL Server connection attempts fail.
    ///
    /// By returning a non-null IHostBuilder we take the *direct* path
    /// (see WebApplicationFactory.EnsureServer). In that path
    /// <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}
    /// .ConfigureHostBuilder(IHostBuilder)"/> is called, which:
    ///   1. Registers a ConfigureWebHost callback containing SetContentRoot,
    ///      the _configuration delegate (= our ConfigureWebHost override),
    ///      and UseTestServer().
    /// These callbacks ARE replayed when the host is built.
    /// UseStartup&lt;Program&gt; reuses the service registrations and HTTP
    /// pipeline from Program.Configuration.cs (a partial class of Program),
    /// so we don't duplicate the bootstrap logic from Program.cs.
    /// </summary>
    protected override IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder(Array.Empty<string>())
            .UseEnvironment("Testing")
            .ConfigureLogging(logging =>
            {
                // The default Windows EventLog provider requires elevated event
                // source access in the sandbox. Tests should report startup errors
                // through the console provider instead of masking them with that
                // platform permission failure.
                logging.ClearProviders();
                logging.AddConsole();
            })
            .ConfigureWebHostDefaults(webHostBuilder =>
            {
                webHostBuilder.UseStartup<Program>();
            });
    }

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
            });
        });

        builder.ConfigureServices(services =>
        {
            // Re-register the per-module DbContexts against the
            // in-memory SQLite databases used by tests. RemoveAll is essential
            // here — EF Core's AddDbContext internally uses TryAdd (not Add) for
            // DbContextOptions<T>, which silently skips when the Program's
            // Add...Module calls have already registered SQL Server options.
            ApplySqliteRegistrations(services);
        });
    }

    /// <summary>
    /// Override CreateHost to re-apply SQLite registrations at the IHostBuilder level.
    /// This guarantees the test registrations run AFTER the Program's configuration
    /// code (executed inside HostBuilder.Build), ensuring SQLite wins over SQL Server.
    /// </summary>
    protected override IHost CreateHost(IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices(services => ApplySqliteRegistrations(services));

        return base.CreateHost(hostBuilder);
    }

    /// <summary>
    /// Removes every existing registration for the module DbContexts and re-registers
    /// them against the shared in-memory SQLite databases. Also wires up test-only
    /// stubs (payment gateway, publish fee, health-check suppression).
    /// </summary>
    private void ApplySqliteRegistrations(IServiceCollection services)
    {
        // Tests run in an isolated sandbox where the user's Windows DPAPI key
        // directory may contain keys this process cannot decrypt or write.
        // A process-local provider is sufficient for test antiforgery/session data.
        services.RemoveAll<IDataProtectionProvider>();
        services.AddSingleton<IDataProtectionProvider, EphemeralDataProtectionProvider>();

        // Identity module
        services.RemoveAll<ApplicationDbContext>();
        services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
        services.RemoveAll<IdentityDbContext>();
        services.RemoveAll<DbContextOptions<IdentityDbContext>>();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(InMemoryIdentityProvider));
        services.AddDbContext<IdentityDbContext>(options => options.UseSqlite(InMemoryIdentityProvider));

        // Other modules — full RemoveAll on the context type AND its DbContextOptions
        // so that EF Core's internal TryAdd does not skip our SQLite registration when
        // the Program's Add...Module calls already registered SQL Server options.
        services.RemoveAll<CandidateDbContext>();
        services.RemoveAll<DbContextOptions<CandidateDbContext>>();
        services.RemoveAll<JobPostingDbContext>();
        services.RemoveAll<DbContextOptions<JobPostingDbContext>>();
        services.RemoveAll<EmployerDbContext>();
        services.RemoveAll<DbContextOptions<EmployerDbContext>>();
        services.RemoveAll<ReferenceDataDbContext>();
        services.RemoveAll<DbContextOptions<ReferenceDataDbContext>>();
        services.RemoveAll<PaymentDbContext>();
        services.RemoveAll<DbContextOptions<PaymentDbContext>>();
        services.RemoveAll<NotificationDbContext>();
        services.RemoveAll<DbContextOptions<NotificationDbContext>>();
        services.RemoveAll<MessagesDbContext>();
        services.RemoveAll<DbContextOptions<MessagesDbContext>>();
        services.RemoveAll<IEmailService>();
        services.AddDbContext<CandidateDbContext>(options => options.UseSqlite(InMemoryDatabaseProvider));
        services.AddDbContext<JobPostingDbContext>(options => options.UseSqlite(InMemoryDatabaseProvider));
        services.AddDbContext<EmployerDbContext>(options => options.UseSqlite(InMemoryDatabaseProvider));
        services.AddDbContext<ReferenceDataDbContext>(options => options.UseSqlite(InMemoryDatabaseProvider));
        services.AddDbContext<PaymentDbContext>(options => options.UseSqlite(InMemoryDatabaseProvider));
        services.AddDbContext<NotificationDbContext>(options => options.UseSqlite(InMemoryDatabaseProvider));
        services.AddDbContext<MessagesDbContext>(options => options.UseSqlite(InMemoryDatabaseProvider));
        services.AddScoped<IEmailService, FailingMessageEmailService>();

        // Payments module: in-memory SQLite, fee-free by default so existing
        // job-lifecycle tests publish immediately. Set a non-zero fee in
        // payment tests to exercise the gateway flow.
        services.Configure<PaymentOptions>(options =>
        {
            options.PublishFeeAmount = PublishFeeAmount;
            options.ZarinPal.Sandbox = true;
            options.ZarinPal.CallbackUrl = "https://jobs.example.test/employer/payments/return";
        });
        services.AddSingleton<IZarinPalClient, StubZarinPalClient>();
        services.AddScoped<IPaymentService, PaymentService>();

        // Clear health-check registrations (they try to connect to SQL Server).
        services.PostConfigure<HealthCheckServiceOptions>(options =>
        {
            options.Registrations.Clear();
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
        var notificationDbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        var messagesDbContext = scope.ServiceProvider.GetRequiredService<MessagesDbContext>();

        // Database.EnsureCreatedAsync is a no-op once ANY table already exists in the
        // shared in-memory database: its CreateDatabase step sees sqlite_master is
        // non-empty and skips CreateTables. With several DbContexts sharing one SQLite
        // cache, only whichever context ran first (while the DB was still empty) actually
        // created tables (Identity + JobPosting in practice); every other module's schema
        // was silently skipped, surfacing as "no such table: JobCategories" -> HTTP 500.
        //
        // Generate the model-derived CREATE script for each context and execute it
        // explicitly so all schemas are built. The SQLite provider still strips the
        // logical schemas (reference_data, jobs, identity, ...) it ignores, exactly as
        // EnsureCreated does, so the generated tables are unqualified.
        await _jobDatabaseLock.WaitAsync();
        try
        {
            if (!_jobDatabaseInitialized)
            {
                // FK enforcement is off in the SQLite provider's default mode, so
                // cross-context foreign keys do not block table creation regardless of
                // the order in which the schemas are created.
                await EnsureSchemaAsync(identityDbContext);
                await EnsureSchemaAsync(referenceDataDbContext);
                await EnsureSchemaAsync(employerDbContext);
                await EnsureSchemaAsync(candidateDbContext);
                await EnsureSchemaAsync(jobPostingDbContext);
                await EnsureSchemaAsync(paymentDbContext);
                await EnsureSchemaAsync(notificationDbContext);
                await EnsureSchemaAsync(messagesDbContext);
                await EnsureSchemaAsync(applicationDbContext);
                _jobDatabaseInitialized = true;
            }
        }
        finally
        {
            _jobDatabaseLock.Release();
        }

        await IdentitySeedData.SeedAsync(Services);
    }

    /// <summary>
    /// Creates the database schema for <paramref name="context"/>'s model without the
    /// "database already exists" short-circuit that <see cref="Database.EnsureCreated"/>
    /// imposes, which skips <c>CreateTables</c> when the shared in-memory database already
    /// holds tables created by another context.
    /// </summary>
    private static async Task EnsureSchemaAsync(DbContext context)
    {
        var script = context.Database.GenerateCreateScript();
        if (!string.IsNullOrWhiteSpace(script))
        {
            await context.Database.ExecuteSqlRawAsync(script);
        }
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

