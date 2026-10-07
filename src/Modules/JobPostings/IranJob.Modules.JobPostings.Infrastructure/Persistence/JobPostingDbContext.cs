using IranJob.Modules.JobPostings.Domain.Entities;
using IranJob.Modules.Candidates.Domain.Entities;
using IranJob.Modules.ReferenceData.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using EmployerProfileEntity = IranJob.Modules.EmployerProfile.Domain.Entities.EmployerProfile;

namespace IranJob.Modules.JobPostings.Infrastructure.Persistence;

public sealed class JobPostingDbContext(DbContextOptions<JobPostingDbContext> options) : DbContext(options)
{
    public DbSet<JobPosting> JobPostings => Set<JobPosting>();
    public DbSet<JobPostingSkill> JobPostingSkills => Set<JobPostingSkill>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("jobs");
        base.OnModelCreating(builder);
        ConfigureExternalEntities(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(JobPostingDbContext).Assembly);
    }

    private static void ConfigureExternalEntities(ModelBuilder builder)
    {
        builder.Entity<CandidateProfile>(entity =>
        {
            entity.ToTable("CandidateProfiles", "candidates", table => table.ExcludeFromMigrations());
            entity.HasKey(profile => profile.Id);
            entity.Ignore(profile => profile.UserId);
            entity.Ignore(profile => profile.Headline);
            entity.Ignore(profile => profile.Biography);
            entity.Ignore(profile => profile.DateOfBirth);
            entity.Ignore(profile => profile.Gender);
            entity.Ignore(profile => profile.City);
            entity.Ignore(profile => profile.Province);
            entity.Ignore(profile => profile.LinkedInUrl);
            entity.Ignore(profile => profile.GitHubUrl);
            entity.Ignore(profile => profile.PortfolioUrl);
            entity.Ignore(profile => profile.ExpectedSalary);
            entity.Ignore(profile => profile.SalaryType);
            entity.Ignore(profile => profile.EmploymentStatus);
            entity.Ignore(profile => profile.Availability);
            entity.Ignore(profile => profile.MilitaryStatus);
            entity.Ignore("CreatedAt");
            entity.Ignore("UpdatedAt");
        });

        builder.Entity<EmployerProfileEntity>(entity =>
        {
            entity.ToTable("EmployerProfiles", "employers", table => table.ExcludeFromMigrations());
            entity.HasKey(profile => profile.Id);
        });

        builder.Entity<JobCategory>(entity =>
        {
            entity.ToTable("JobCategories", "reference_data", table => table.ExcludeFromMigrations());
            entity.HasKey(category => category.Id);
        });

        builder.Entity<Skill>(entity =>
        {
            entity.ToTable("Skills", "reference_data", table => table.ExcludeFromMigrations());
            entity.HasKey(skill => skill.Id);
        });

        builder.Entity<Country>(entity =>
        {
            entity.ToTable("Countries", "reference_data", table => table.ExcludeFromMigrations());
            entity.HasKey(country => country.Id);
        });

        builder.Entity<Province>(entity =>
        {
            entity.ToTable("Provinces", "reference_data", table => table.ExcludeFromMigrations());
            entity.HasKey(province => province.Id);
            entity.HasOne(province => province.Country).WithMany().HasForeignKey(province => province.CountryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<City>(entity =>
        {
            entity.ToTable("Cities", "reference_data", table => table.ExcludeFromMigrations());
            entity.HasKey(city => city.Id);
            entity.HasOne(city => city.Province).WithMany().HasForeignKey(city => city.ProvinceId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
