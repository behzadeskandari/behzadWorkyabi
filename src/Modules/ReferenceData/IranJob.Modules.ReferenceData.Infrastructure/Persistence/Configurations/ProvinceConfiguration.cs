using IranJob.Modules.ReferenceData.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IranJob.Modules.ReferenceData.Infrastructure.Persistence.Configurations;

public sealed class ProvinceConfiguration : IEntityTypeConfiguration<Province>
{
    public void Configure(EntityTypeBuilder<Province> builder)
    {
        builder.ToTable("Provinces", "reference_data");
        builder.HasKey(province => province.Id);
        builder.Property(province => province.Name).HasMaxLength(150).IsRequired();
        builder.Property(province => province.NormalizedName).HasMaxLength(150).IsRequired();
        builder.HasOne(province => province.Country).WithMany().HasForeignKey(province => province.CountryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(province => new { province.CountryId, province.NormalizedName }).IsUnique();
        builder.HasIndex(province => province.IsActive);
    }
}
