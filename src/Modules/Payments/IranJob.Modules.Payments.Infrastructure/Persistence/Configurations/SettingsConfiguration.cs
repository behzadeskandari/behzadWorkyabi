using IranJob.Modules.Payments.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IranJob.Modules.Payments.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <c>settings</c> table (schema: payments).
/// Holds only non-secret, explicitly approved configuration values.
/// </summary>
public sealed class SettingsConfiguration : IEntityTypeConfiguration<Settings>
{
    public void Configure(EntityTypeBuilder<Settings> builder)
    {
        builder.ToTable("Settings", "payments");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.Value)
            .HasMaxLength(2000)
            .IsRequired();

        builder.HasIndex(s => s.Name)
            .IsUnique();
    }
}
