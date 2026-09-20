using IranJob.Modules.Candidates.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IranJob.Modules.Candidates.Infrastructure.Persistence;

public sealed class CandidateDbContext(DbContextOptions<CandidateDbContext> options) : DbContext(options)
{
    public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("candidates");
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(CandidateDbContext).Assembly);
    }
}
