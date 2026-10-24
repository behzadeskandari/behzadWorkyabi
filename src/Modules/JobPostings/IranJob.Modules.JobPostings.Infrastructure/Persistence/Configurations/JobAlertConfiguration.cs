using IranJob.Modules.Candidates.Domain.Entities;
using IranJob.Modules.JobPostings.Domain.Entities;
using IranJob.Modules.ReferenceData.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IranJob.Modules.JobPostings.Infrastructure.Persistence.Configurations;

public sealed class JobAlertConfiguration : IEntityTypeConfiguration<JobAlert>
{
    public void Configure(EntityTypeBuilder<JobAlert> builder)
    {
        builder.ToTable("JobAlerts", "jobs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.WorkArrangement).HasMaxLength(50);
        builder.HasIndex(x => new { x.CandidateProfileId, x.IsEnabled });
        builder.HasIndex(x => new { x.IsEnabled, x.CategoryId, x.CityId, x.WorkArrangement });
        builder.HasOne<CandidateProfile>().WithMany().HasForeignKey(x => x.CandidateProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<JobCategory>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<City>().WithMany().HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Skills).WithOne(x => x.JobAlert).HasForeignKey(x => x.JobAlertId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class JobAlertSkillConfiguration : IEntityTypeConfiguration<JobAlertSkill>
{
    public void Configure(EntityTypeBuilder<JobAlertSkill> builder)
    {
        builder.ToTable("JobAlertSkills", "jobs");
        builder.HasKey(x => new { x.JobAlertId, x.SkillId });
        builder.HasOne<Skill>().WithMany().HasForeignKey(x => x.SkillId).OnDelete(DeleteBehavior.Restrict);
    }
}
