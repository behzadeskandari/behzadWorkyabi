using IranJob.Modules.Candidates.Domain.Entities;
using IranJob.Modules.JobPostings.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IranJob.Modules.JobPostings.Infrastructure.Persistence.Configurations;

public sealed class JobApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> builder)
    {
        builder.ToTable("JobApplications", "jobs");
        builder.HasKey(application => application.Id);
        builder.Property(application => application.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(application => application.CreatedAt).IsRequired();

        builder.HasOne<CandidateProfile>().WithMany().HasForeignKey(application => application.CandidateProfileId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<JobPosting>().WithMany().HasForeignKey(application => application.JobPostingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(application => new { application.CandidateProfileId, application.JobPostingId })
            .IsUnique()
            .HasDatabaseName("IX_JobApplications_CandidateProfileId_JobPostingId");
        builder.HasIndex(application => application.JobPostingId)
            .HasDatabaseName("IX_JobApplications_JobPostingId");
    }
}
