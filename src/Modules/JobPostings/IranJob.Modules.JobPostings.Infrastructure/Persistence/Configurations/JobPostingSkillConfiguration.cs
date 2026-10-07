using IranJob.Modules.JobPostings.Domain.Entities;
using IranJob.Modules.ReferenceData.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IranJob.Modules.JobPostings.Infrastructure.Persistence.Configurations;

public sealed class JobPostingSkillConfiguration : IEntityTypeConfiguration<JobPostingSkill>
{
    public void Configure(EntityTypeBuilder<JobPostingSkill> builder)
    {
        builder.ToTable("JobPostingSkills", "jobs");
        builder.HasKey(item => new { item.JobPostingId, item.SkillId });
        builder.HasOne<Skill>().WithMany().HasForeignKey(item => item.SkillId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => item.SkillId);
    }
}
