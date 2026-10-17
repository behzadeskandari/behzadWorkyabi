using System;
using IranJob.Modules.Candidates.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IranJob.Modules.Candidates.Infrastructure.Persistence.Configurations;

public sealed class ResumeSkillConfiguration : IEntityTypeConfiguration<ResumeSkill>
{
    public void Configure(EntityTypeBuilder<ResumeSkill> builder)
    {
        builder.ToTable("ResumeSkills");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.ResumeId).IsRequired();
        builder.Property(entry => entry.SkillId).IsRequired();
        builder.Property(entry => entry.SkillName).IsRequired().HasMaxLength(200);
        builder.Property(entry => entry.ProficiencyLevel).HasMaxLength(100);
        builder.Property(entry => entry.YearsOfExperience);
        builder.Property(entry => entry.SortOrder).IsRequired();
        builder.HasIndex(entry => new { entry.ResumeId, entry.SkillId }).IsUnique();
        builder.HasIndex(entry => new { entry.ResumeId, entry.SortOrder });
    }
}

public sealed class ResumeLanguageConfiguration : IEntityTypeConfiguration<ResumeLanguage>
{
    public void Configure(EntityTypeBuilder<ResumeLanguage> builder)
    {
        builder.ToTable("ResumeLanguages");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.ResumeId).IsRequired();
        builder.Property(entry => entry.LanguageName).IsRequired().HasMaxLength(100);
        builder.Property(entry => entry.ProficiencyLevel).IsRequired().HasMaxLength(100);
        builder.Property(entry => entry.SortOrder).IsRequired();
        builder.HasIndex(entry => new { entry.ResumeId, entry.SortOrder });
    }
}

public sealed class ResumeProjectConfiguration : IEntityTypeConfiguration<ResumeProject>
{
    public void Configure(EntityTypeBuilder<ResumeProject> builder)
    {
        builder.ToTable("ResumeProjects");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.ResumeId).IsRequired();
        builder.Property(entry => entry.Title).IsRequired().HasMaxLength(200);
        builder.Property(entry => entry.Description).HasMaxLength(4000);
        builder.Property(entry => entry.Url).HasMaxLength(500);
        builder.Property(entry => entry.StartDate).HasColumnType("date");
        builder.Property(entry => entry.EndDate).HasColumnType("date");
        builder.Property(entry => entry.IsCurrent).IsRequired();
        builder.Property(entry => entry.SortOrder).IsRequired();
        builder.HasIndex(entry => new { entry.ResumeId, entry.SortOrder });
    }
}

public sealed class ResumeCertificationConfiguration : IEntityTypeConfiguration<ResumeCertification>
{
    public void Configure(EntityTypeBuilder<ResumeCertification> builder)
    {
        builder.ToTable("ResumeCertifications");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.ResumeId).IsRequired();
        builder.Property(entry => entry.Name).IsRequired().HasMaxLength(200);
        builder.Property(entry => entry.IssuingOrganization).HasMaxLength(200);
        builder.Property(entry => entry.IssueDate).HasColumnType("date");
        builder.Property(entry => entry.ExpiryDate).HasColumnType("date");
        builder.Property(entry => entry.CertificateUrl).HasMaxLength(500);
        builder.Property(entry => entry.SortOrder).IsRequired();
        builder.HasIndex(entry => new { entry.ResumeId, entry.SortOrder });
    }
}

public sealed class ResumePortfolioLinkConfiguration : IEntityTypeConfiguration<ResumePortfolioLink>
{
    public void Configure(EntityTypeBuilder<ResumePortfolioLink> builder)
    {
        builder.ToTable("ResumePortfolioLinks");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.ResumeId).IsRequired();
        builder.Property(entry => entry.Title).IsRequired().HasMaxLength(200);
        builder.Property(entry => entry.Url).IsRequired().HasMaxLength(500);
        builder.Property(entry => entry.Description).HasMaxLength(1000);
        builder.Property(entry => entry.SortOrder).IsRequired();
        builder.HasIndex(entry => new { entry.ResumeId, entry.SortOrder });
    }
}