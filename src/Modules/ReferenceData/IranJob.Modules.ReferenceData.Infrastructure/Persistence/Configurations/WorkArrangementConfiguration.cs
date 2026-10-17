using IranJob.Modules.ReferenceData.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IranJob.Modules.ReferenceData.Infrastructure.Persistence.Configurations;

public sealed class WorkArrangementConfiguration : IEntityTypeConfiguration<WorkArrangement>
{
    public void Configure(EntityTypeBuilder<WorkArrangement> builder)
    {
        builder.ToTable("WorkArrangements", "reference_data");
        builder.HasKey(wa => wa.Id);
        builder.Property(wa => wa.Value).HasMaxLength(50).IsRequired();
        builder.Property(wa => wa.NormalizedValue).HasMaxLength(50).IsRequired();
        builder.Property(wa => wa.Name).HasMaxLength(150).IsRequired();
        builder.Property(wa => wa.NormalizedName).HasMaxLength(150).IsRequired();
        builder.Property(wa => wa.Description).HasMaxLength(1000);
        builder.HasIndex(wa => wa.NormalizedValue).IsUnique();
        builder.HasIndex(wa => wa.IsActive);
    }
}