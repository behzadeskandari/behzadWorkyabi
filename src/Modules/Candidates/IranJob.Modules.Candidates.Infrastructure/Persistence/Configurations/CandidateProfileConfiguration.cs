using IranJob.Modules.Candidates.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IranJob.Modules.Candidates.Infrastructure.Persistence.Configurations;

public sealed class CandidateProfileConfiguration : IEntityTypeConfiguration<CandidateProfile>
{
    public void Configure(EntityTypeBuilder<CandidateProfile> builder)
    {
        builder.ToTable("CandidateProfiles");

        builder.HasKey(profile => profile.Id);

        builder.Property(profile => profile.UserId)
            .IsRequired();

        // One candidate profile per user. The relationship to the Identity user is by
        // identifier only: the Candidates module must not depend on Identity entities.
        builder.HasIndex(profile => profile.UserId)
            .IsUnique()
            .HasDatabaseName("IX_CandidateProfiles_UserId");

        builder.Property(profile => profile.Headline)
            .HasMaxLength(200);

        builder.Property(profile => profile.Biography)
            .HasMaxLength(4000);

        builder.Property(profile => profile.City)
            .HasMaxLength(100);

        builder.Property(profile => profile.Province)
            .HasMaxLength(100);

        builder.Property(profile => profile.Phone)
            .HasMaxLength(20);

        builder.Property(profile => profile.Email)
            .HasMaxLength(256);

        builder.Property(profile => profile.LinkedInUrl)
            .HasMaxLength(500);

        builder.Property(profile => profile.GitHubUrl)
            .HasMaxLength(500);

        builder.Property(profile => profile.PortfolioUrl)
            .HasMaxLength(500);

        builder.Property(profile => profile.ExpectedSalary)
            .HasColumnType("decimal(18,2)");

        builder.Property(profile => profile.Gender)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(profile => profile.SalaryType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(profile => profile.EmploymentStatus)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(profile => profile.Availability)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(profile => profile.MilitaryStatus)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(profile => profile.CreatedAt)
            .IsRequired();

        builder.Property(profile => profile.UpdatedAt)
            .IsRequired();
    }
}
