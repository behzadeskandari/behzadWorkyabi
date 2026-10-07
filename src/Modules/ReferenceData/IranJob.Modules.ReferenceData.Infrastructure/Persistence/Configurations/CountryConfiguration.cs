using IranJob.Modules.ReferenceData.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IranJob.Modules.ReferenceData.Infrastructure.Persistence.Configurations;

public sealed class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("Countries", "reference_data");
        builder.HasKey(country => country.Id);
        builder.Property(country => country.Name).HasMaxLength(150).IsRequired();
        builder.Property(country => country.NormalizedName).HasMaxLength(150).IsRequired();
        builder.Property(country => country.Code).HasMaxLength(3).IsRequired();
        builder.Property(country => country.NormalizedCode).HasMaxLength(3).IsRequired();
        builder.HasIndex(country => country.NormalizedCode).IsUnique();
        builder.HasIndex(country => country.IsActive);
    }
}
