using IranJob.Modules.Candidates.Domain.Entities;
using IranJob.Modules.JobPostings.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IranJob.Modules.JobPostings.Infrastructure.Persistence.Configurations;

public sealed class SavedJobConfiguration : IEntityTypeConfiguration<SavedJob>
{
    public void Configure(EntityTypeBuilder<SavedJob> builder)
    {
        builder.ToTable("SavedJobs", "jobs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SavedAt).IsRequired();
        builder.HasIndex(x => new { x.CandidateProfileId, x.JobPostingId }).IsUnique();
        builder.HasIndex(x => new { x.CandidateProfileId, x.SavedAt });
        builder.HasOne<CandidateProfile>().WithMany().HasForeignKey(x => x.CandidateProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<JobPosting>().WithMany().HasForeignKey(x => x.JobPostingId).OnDelete(DeleteBehavior.Cascade);
    }
}
