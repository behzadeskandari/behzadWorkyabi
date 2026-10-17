using IranJob.Modules.Candidates.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IranJob.Modules.Candidates.Infrastructure.Persistence;

public sealed class CandidateDbContext(DbContextOptions<CandidateDbContext> options) : DbContext(options)
{
    public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();

    public DbSet<CandidateResume> CandidateResumes => Set<CandidateResume>();
    public DbSet<ResumeWorkExperience> ResumeWorkExperiences => Set<ResumeWorkExperience>();
    public DbSet<ResumeEducation> ResumeEducations => Set<ResumeEducation>();
    public DbSet<ResumeSkill> ResumeSkills => Set<ResumeSkill>();
    public DbSet<ResumeLanguage> ResumeLanguages => Set<ResumeLanguage>();
    public DbSet<ResumeProject> ResumeProjects => Set<ResumeProject>();
    public DbSet<ResumeCertification> ResumeCertifications => Set<ResumeCertification>();
    public DbSet<ResumePortfolioLink> ResumePortfolioLinks => Set<ResumePortfolioLink>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("candidates");
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(CandidateDbContext).Assembly);
    }
}
