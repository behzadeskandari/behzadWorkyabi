using IranJob.Modules.ReferenceData.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IranJob.Modules.ReferenceData.Infrastructure.Persistence;

public sealed class ReferenceDataDbContext(DbContextOptions<ReferenceDataDbContext> options) : DbContext(options)
{
    public DbSet<JobCategory> JobCategories => Set<JobCategory>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Province> Provinces => Set<Province>();
    public DbSet<City> Cities => Set<City>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("reference_data");
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ReferenceDataDbContext).Assembly);
    }
}
