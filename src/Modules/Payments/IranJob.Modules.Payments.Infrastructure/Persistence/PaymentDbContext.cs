using IranJob.Modules.Payments.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IranJob.Modules.Payments.Infrastructure.Persistence;

/// <summary>
/// Per-module DbContext owning the <c>payments</c> schema. Foreign-owned tables
/// (e.g. <c>employers.EmployerProfiles</c>) are never owned here; only the <see cref="Payment"/>
/// aggregate is mapped, so the module stays decoupled from the EmployerProfile schema.
/// </summary>
public sealed class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("payments");
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(PaymentDbContext).Assembly);
    }
}
