using IranJob.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IranJob.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class AuthenticationAuditEntryConfiguration : IEntityTypeConfiguration<AuthenticationAuditEntry>
{
    public void Configure(EntityTypeBuilder<AuthenticationAuditEntry> builder)
    {
        builder.ToTable("AuthenticationAuditEntries");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.EventType).HasMaxLength(100).IsRequired();
        builder.Property(entry => entry.CorrelationId).HasMaxLength(100);
        builder.Property(entry => entry.IpAddress).HasMaxLength(64);
        builder.Property(entry => entry.UserAgent).HasMaxLength(512);
        builder.Property(entry => entry.Metadata).HasMaxLength(2000);
        builder.HasIndex(entry => entry.Timestamp);
        builder.HasIndex(entry => entry.UserId);
        builder.HasIndex(entry => entry.EventType);
    }
}
