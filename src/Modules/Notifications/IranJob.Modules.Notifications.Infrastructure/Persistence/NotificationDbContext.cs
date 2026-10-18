using IranJob.Modules.Notifications.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IranJob.Modules.Notifications.Infrastructure.Persistence;

public sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options)
    : DbContext(options)
{
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("notifications");
        base.OnModelCreating(builder);
        ApplyConfigurationsFromAssembly(builder);
    }

    private static void ApplyConfigurationsFromAssembly(ModelBuilder builder)
    {
        var assembly = typeof(NotificationDbContext).Assembly;
        builder.ApplyConfigurationsFromAssembly(assembly);
    }
}
