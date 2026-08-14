using Microsoft.EntityFrameworkCore;

namespace IranJob.BuildingBlocks.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("infra");
        base.OnModelCreating(modelBuilder);
    }
}
