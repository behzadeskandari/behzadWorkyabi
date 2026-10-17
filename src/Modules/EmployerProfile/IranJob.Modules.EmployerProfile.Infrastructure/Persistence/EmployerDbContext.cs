using EmployerProfileEntity = IranJob.Modules.EmployerProfile.Domain.EmployerProfile.EmployerProfile;
using Microsoft.EntityFrameworkCore;

namespace IranJob.Modules.EmployerProfile.Infrastructure.Persistence;

/// <summary>
/// Per-module DbContext owning the <c>employers</c> schema. Only aggregate roots
/// and owned types declared by this module live here; cross-module references
/// (e.g. JobPostings' FK to EmployerProfile) are configured as unowned FKs in
/// the referencing module's model so no table duplication occurs.
/// </summary>
public sealed class EmployerDbContext(DbContextOptions<EmployerDbContext> options) : DbContext(options)
{
    public DbSet<EmployerProfileEntity> EmployerProfiles => Set<EmployerProfileEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("employers");
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(EmployerDbContext).Assembly);
    }
}

