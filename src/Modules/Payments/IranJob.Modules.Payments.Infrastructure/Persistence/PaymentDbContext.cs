using IranJob.Modules.Payments.Domain.Entities;
using IranJob.Modules.Payments.Domain.Settings;
using Microsoft.EntityFrameworkCore;

namespace IranJob.Modules.Payments.Infrastructure.Persistence;

/// <summary>
/// Per-module DbContext owning the <c>payments</c> schema. Foreign-owned tables
/// (e.g. <c>employers.EmployerProfiles</c>) are never owned here; the module owns
/// the <see cref="Payment"/> aggregate and <see cref="Settings"/> only, so it stays
/// decoupled from the Identity and EmployerProfile schemas.
/// </summary>
public sealed class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Settings> Settings => Set<Settings>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("payments");
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(PaymentDbContext).Assembly);
    }
}
