using IranJob.Modules.ReferenceData.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IranJob.Modules.ReferenceData.Infrastructure.Persistence.Configurations;

public sealed class EmploymentTypeConfiguration : IEntityTypeConfiguration<EmploymentType>
{
    public void Configure(EntityTypeBuilder<EmploymentType> builder)
    {
        builder.ToTable("EmploymentTypes", "reference_data");
        builder.HasKey(et => et.Id);
        builder.Property(et => et.Value).HasMaxLength(50).IsRequired();
        builder.Property(et => et.NormalizedValue).HasMaxLength(50).IsRequired();
        builder.Property(et => et.Name).HasMaxLength(150).IsRequired();
        builder.Property(et => et.NormalizedName).HasMaxLength(150).IsRequired();
        builder.Property(et => et.Description).HasMaxLength(1000);
        builder.HasIndex(et => et.NormalizedValue).IsUnique();
        builder.HasIndex(et => et.IsActive);
    }
}