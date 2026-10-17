using IranJob.Modules.Candidates.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IranJob.Modules.Candidates.Infrastructure.Persistence.Configurations;

public sealed class CandidateResumeConfiguration : IEntityTypeConfiguration<CandidateResume>
{
    public void Configure(EntityTypeBuilder<CandidateResume> builder)
    {
        builder.ToTable("CandidateResumes");
        builder.HasKey(resume => resume.Id);
        builder.Property(resume => resume.UserId).IsRequired();
        builder.HasIndex(resume => resume.UserId).IsUnique().HasDatabaseName("IX_CandidateResumes_UserId");
        builder.Property(resume => resume.ProfessionalSummary).HasMaxLength(2000);
        builder.Property(resume => resume.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(resume => resume.CompletenessScore).IsRequired();
        builder.Property(resume => resume.CvFileName).HasMaxLength(256);
        builder.Property(resume => resume.CvFileContentType).HasMaxLength(100);
        builder.Property(resume => resume.CvFileSizeBytes);
        builder.Property(resume => resume.CvUploadedAt);
        builder.Property(resume => resume.CreatedAt).IsRequired();
        builder.Property(resume => resume.UpdatedAt).IsRequired();

        builder.HasMany(resume => resume.WorkExperiences).WithOne().HasForeignKey(e => e.ResumeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(resume => resume.Educations).WithOne().HasForeignKey(e => e.ResumeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(resume => resume.Skills).WithOne().HasForeignKey(e => e.ResumeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(resume => resume.Languages).WithOne().HasForeignKey(e => e.ResumeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(resume => resume.Projects).WithOne().HasForeignKey(e => e.ResumeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(resume => resume.Certifications).WithOne().HasForeignKey(e => e.ResumeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(resume => resume.PortfolioLinks).WithOne().HasForeignKey(e => e.ResumeId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ResumeWorkExperienceConfiguration : IEntityTypeConfiguration<ResumeWorkExperience>
{
    public void Configure(EntityTypeBuilder<ResumeWorkExperience> builder)
    {
        builder.ToTable("ResumeWorkExperiences");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.ResumeId).IsRequired();
        builder.Property(entry => entry.Company).IsRequired().HasMaxLength(200);
        builder.Property(entry => entry.JobTitle).IsRequired().HasMaxLength(200);
        builder.Property(entry => entry.Description).HasMaxLength(2000);
        builder.Property(entry => entry.StartDate).HasColumnType("date");
        builder.Property(entry => entry.EndDate).HasColumnType("date");
        builder.Property(entry => entry.IsCurrent).IsRequired();
        builder.Property(entry => entry.SortOrder).IsRequired();
        builder.HasIndex(entry => new { entry.ResumeId, entry.SortOrder });
    }
}

public sealed class ResumeEducationConfiguration : IEntityTypeConfiguration<ResumeEducation>
{
    public void Configure(EntityTypeBuilder<ResumeEducation> builder)
    {
        builder.ToTable("ResumeEducations");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.ResumeId).IsRequired();
        builder.Property(entry => entry.Institution).IsRequired().HasMaxLength(200);
        builder.Property(entry => entry.Degree).IsRequired().HasMaxLength(200);
        builder.Property(entry => entry.FieldOfStudy).HasMaxLength(200);
        builder.Property(entry => entry.Grade).HasMaxLength(100);
        builder.Property(entry => entry.StartDate).HasColumnType("date");
        builder.Property(entry => entry.EndDate).HasColumnType("date");
        builder.Property(entry => entry.IsCurrent).IsRequired();
        builder.Property(entry => entry.SortOrder).IsRequired();
        builder.HasIndex(entry => new { entry.ResumeId, entry.SortOrder });
    }
}