using EmployerProfileEntity = IranJob.Modules.EmployerProfile.Domain.EmployerProfile.EmployerProfile;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IranJob.Modules.EmployerProfile.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the EmployerProfile aggregate root. Mirrors the cross-module ownership
/// pattern: the EmployerProfile module owns this table (schema <c>employers</c>)
/// and other modules reference it only for reads / FK ownership.
/// </summary>
public sealed class EmployerProfileConfiguration : IEntityTypeConfiguration<EmployerProfileEntity>
{
    public void Configure(EntityTypeBuilder<EmployerProfileEntity> builder)
    {
        builder.ToTable("EmployerProfiles", "employers");
        builder.HasKey(profile => profile.Id);

        builder.Property(profile => profile.UserId).IsRequired();
        builder.Property(profile => profile.JobTitle).HasMaxLength(200).IsRequired();
        builder.Property(profile => profile.Biography).HasMaxLength(2000);
        builder.Property(profile => profile.Province).HasMaxLength(100).IsRequired();
        builder.Property(profile => profile.City).HasMaxLength(100).IsRequired();
        builder.Property(profile => profile.LinkedInUrl).HasMaxLength(500);
        builder.Property(profile => profile.PortfolioUrl).HasMaxLength(500);

        builder.Property(profile => profile.CompanyDescription).HasMaxLength(2000);
        builder.Property(profile => profile.WebsiteUrl).HasMaxLength(500);
        builder.Property(profile => profile.Industry).HasMaxLength(100).IsRequired();

        builder.HasIndex(profile => profile.UserId).IsUnique();
    }
}
