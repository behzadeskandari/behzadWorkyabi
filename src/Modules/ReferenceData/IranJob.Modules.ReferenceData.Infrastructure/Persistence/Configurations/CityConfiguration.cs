using IranJob.Modules.ReferenceData.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IranJob.Modules.ReferenceData.Infrastructure.Persistence.Configurations;

public sealed class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("Cities", "reference_data");
        builder.HasKey(city => city.Id);
        builder.Property(city => city.Name).HasMaxLength(150).IsRequired();
        builder.Property(city => city.NormalizedName).HasMaxLength(150).IsRequired();
        builder.HasOne(city => city.Province).WithMany().HasForeignKey(city => city.ProvinceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(city => new { city.ProvinceId, city.NormalizedName }).IsUnique();
        builder.HasIndex(city => city.IsActive);
    }
}
