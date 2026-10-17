using IranJob.Modules.ReferenceData.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IranJob.Modules.ReferenceData.Infrastructure.Persistence.Configurations;

public sealed class SalaryCurrencyConfiguration : IEntityTypeConfiguration<SalaryCurrency>
{
    public void Configure(EntityTypeBuilder<SalaryCurrency> builder)
    {
        builder.ToTable("SalaryCurrencies", "reference_data");
        builder.HasKey(sc => sc.Id);
        builder.Property(sc => sc.Code).HasMaxLength(3).IsRequired();
        builder.Property(sc => sc.NormalizedCode).HasMaxLength(3).IsRequired();
        builder.Property(sc => sc.Name).HasMaxLength(150).IsRequired();
        builder.Property(sc => sc.NormalizedName).HasMaxLength(150).IsRequired();
        builder.Property(sc => sc.Symbol).HasMaxLength(10);
        builder.HasIndex(sc => sc.NormalizedCode).IsUnique();
        builder.HasIndex(sc => sc.IsActive);
    }
}