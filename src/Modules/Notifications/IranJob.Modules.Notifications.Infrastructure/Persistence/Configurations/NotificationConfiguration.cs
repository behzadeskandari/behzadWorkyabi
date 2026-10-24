using IranJob.Modules.Notifications.Domain.Entities;
using IranJob.Modules.Notifications.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IranJob.Modules.Notifications.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(notification => notification.Id);

        builder.Property(notification => notification.RecipientUserId)
            .IsRequired();
        builder.Property(notification => notification.Type)
            .IsRequired()
            .HasConversion<int>();
        builder.Property(notification => notification.Title)
            .IsRequired()
            .HasMaxLength(500);
        builder.Property(notification => notification.Message)
            .IsRequired()
            .HasMaxLength(2000);
        builder.Property(notification => notification.LocalizedKey)
            .HasMaxLength(200);
        builder.Property(notification => notification.RelatedResourceId);
        builder.Property(notification => notification.IsRead)
            .IsRequired();
        builder.Property(notification => notification.ReadAt);
        builder.Property(notification => notification.CreatedAt)
            .IsRequired();

        // A recipient may have many notifications; indexed for the paged inbox.
        builder.HasIndex(notification => new { notification.RecipientUserId, notification.IsRead, notification.CreatedAt });

        // Idempotency: never insert two notifications of the same type pointing at the
        // same related resource for the same recipient. A repeated dispatch is a no-op.
        builder.HasIndex(notification => new
            {
                notification.RecipientUserId,
                Type = (int)notification.Type,
                notification.RelatedResourceId
            })
            .IsUnique();
    }
}
