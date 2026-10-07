using IranJob.Modules.JobPostings.Domain.Entities;
using IranJob.Modules.ReferenceData.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EmployerProfileEntity = IranJob.Modules.EmployerProfile.Domain.Entities.EmployerProfile;

namespace IranJob.Modules.JobPostings.Infrastructure.Persistence.Configurations;

public sealed class JobPostingConfiguration : IEntityTypeConfiguration<JobPosting>
{
    public void Configure(EntityTypeBuilder<JobPosting> builder)
    {
        builder.ToTable("JobPostings", "jobs");
        builder.HasKey(posting => posting.Id);
        builder.Property(posting => posting.Title).HasMaxLength(200).IsRequired();
        builder.Property(posting => posting.Description).HasMaxLength(10000).IsRequired();
        builder.Property(posting => posting.EmploymentType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(posting => posting.WorkArrangement).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(posting => posting.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(posting => posting.SalaryPeriod).HasConversion<string>().HasMaxLength(50);
        builder.Property(posting => posting.SalaryMinimum).HasColumnType("decimal(18,2)");
        builder.Property(posting => posting.SalaryMaximum).HasColumnType("decimal(18,2)");
        builder.Property(posting => posting.SalaryCurrency).HasMaxLength(3);

        builder.HasOne<EmployerProfileEntity>().WithMany().HasForeignKey(posting => posting.EmployerProfileId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<JobCategory>().WithMany().HasForeignKey(posting => posting.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<City>().WithMany().HasForeignKey(posting => posting.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(posting => posting.Skills).WithOne(skill => skill.JobPosting)
            .HasForeignKey(skill => skill.JobPostingId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(posting => new { posting.EmployerProfileId, posting.Status, posting.CreatedAt });
        builder.HasIndex(posting => new { posting.Status, posting.PublishedAt });
        builder.HasIndex(posting => posting.CategoryId);
        builder.HasIndex(posting => posting.CityId);
        builder.HasIndex(posting => posting.ClosingAt);
    }
}
