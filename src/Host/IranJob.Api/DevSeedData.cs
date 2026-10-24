// © IranJob. Development-only seed data — creates test accounts and sample jobs.
// Compiled in all configurations; SeedAsync returns immediately unless the host
// environment is "Development", so production is never affected.
#nullable enable
using IranJob.Modules.Candidates.Infrastructure.Persistence;
using IranJob.Modules.EmployerProfile.Infrastructure.Persistence;
using IranJob.Modules.Identity.Domain.Constants;
using IranJob.Modules.Identity.Domain.Entities;
using IranJob.Modules.JobPostings.Domain.Entities;
using IranJob.Modules.JobPostings.Domain.Enums;
using IranJob.Modules.JobPostings.Infrastructure.Persistence;
using IranJob.Modules.ReferenceData.Domain.Entities;
using IranJob.Modules.ReferenceData.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IranJob.Api;

public static class DevSeedData
{
    /// <summary>Password shared by all four dev accounts. Meets Identity rules (8+ chars, digit, upper, lower, non-alphan).</summary>
    public const string DevPassword = "DevPass!123";

    private static readonly Guid DevCandidate1UserId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DevEmployer1UserId = new("33333333-3333-3333-3333-333333333333");
    private static readonly Guid DevCandidate2UserId = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid DevEmployer2UserId = new("44444444-4444-4444-4444-444444444444");

    // Reference-data GUIDs seeded by migration 20261007 (Iran locations)
    private static readonly Guid TehranCityId = new("C0A80100-0003-0001-0000-000000000001");
    private static readonly Guid IsfahanCityId = new("C0A80100-0003-0004-0000-000000000401");
    private static readonly Guid MashhadCityId = new("C0A80100-0003-0005-0000-000000000501");
    private static readonly Guid ShirazCityId = new("C0A80100-0003-0006-0000-000000000001");

    // Stable GUIDs for categories and skills created by this seed
    private static readonly Guid CategoryItId = new("A1000000-0001-0001-0001-000000000001");
    private static readonly Guid CategoryDesignId = new("A1000000-0001-0001-0001-000000000002");

    private static readonly Guid SkillCSharpId = new("A2000000-0001-0001-0001-000000000001");
    private static readonly Guid SkillJsId = new("A2000000-0001-0001-0001-000000000002");
    private static readonly Guid SkillAngularId = new("A2000000-0001-0001-0001-000000000003");
    private static readonly Guid SkillReactId = new("A2000000-0001-0001-0001-000000000004");
    private static readonly Guid SkillPythonId = new("A2000000-0001-0001-0001-000000000005");
    private static readonly Guid SkillSqlId = new("A2000000-0001-0001-0001-000000000006");
    private static readonly Guid SkillDockerId = new("A2000000-0001-0001-0001-000000000007");
    private static readonly Guid SkillAwsId = new("A2000000-0001-0001-0001-000000000008");
    private static readonly Guid SkillFigmaId = new("A2000000-0001-0001-0001-000000000009");
    private static readonly Guid SkillUxId = new("A2000000-0001-0001-0001-000000000010");

    private static readonly Guid Employer1ProfileId = new("E1000000-0001-0001-0001-000000000001");
    private static readonly Guid Employer2ProfileId = new("E1000000-0002-0001-0001-000000000001");
    private static readonly Guid Candidate1ProfileId = new("E2000000-0001-0001-0001-000000000001");
    private static readonly Guid Candidate2ProfileId = new("E2000000-0002-0001-0001-000000000001");

    private static readonly Guid[] JobIds =
    [
        new("B1000000-0001-0001-0001-000000000001"),
        new("B1000000-0001-0001-0001-000000000002"),
        new("B1000000-0001-0001-0001-000000000003"),
        new("B1000000-0001-0001-0001-000000000004"),
        new("B1000000-0001-0001-0001-000000000005"),
        new("B1000000-0001-0001-0001-000000000006"),
        new("B1000000-0001-0001-0001-000000000007"),
        new("B1000000-0001-0001-0001-000000000008"),
        new("B1000000-0001-0001-0001-000000000009"),
        new("B1000000-0001-0001-0001-000000000010"),
    ];

    public static readonly string[] DevJobTitles =
    [
        "مهندس نرم‌افزار ارشد - .NET",
        "طراح رابط کاربری (UI/UX)",
        "مهندس بک‌اند - Python",
        "توسعه‌دهنده فرانت‌اند - React",
        "مهندس دیتا‌ساینس و هوش مصنوعی",
        "کارشناس امنیت اطلاعات و سایبری",
        "مهندس فول‌استک",
        "تحلیل‌گر داده و بیزینس اینتلیجنس",
        "کارآموز توسعه وب",
        "مدیر فنی (Tech Lead)",
    ];

    public static readonly DevAccount[] DevAccounts =
    [
        new("candidate1@test.ir", "رامین",  "احمدی",  IdentityRoles.Candidate, DevCandidate1UserId, Candidate1ProfileId),
        new("candidate2@test.ir", "لیلا",   "حسینی",  IdentityRoles.Candidate, DevCandidate2UserId, Candidate2ProfileId),
        new("employer1@test.ir",  "سیاوش", "مظاهری", IdentityRoles.Employer, DevEmployer1UserId,  Employer1ProfileId),
        new("employer2@test.ir",  "نگار",   "کریمی",  IdentityRoles.Employer, DevEmployer2UserId,  Employer2ProfileId),
    ];

    public record DevAccount(string Email, string FirstName, string LastName, string Role, Guid UserId, Guid ProfileId);

    // ─── Entry point ───

    public static async Task SeedAsync(IServiceProvider services, IHostEnvironment env, ILogger logger)
    {
        if (!env.IsDevelopment()) return;

        await using var scope = services.CreateAsyncScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var refDbContext = scope.ServiceProvider.GetRequiredService<ReferenceDataDbContext>();
        var empDbContext = scope.ServiceProvider.GetRequiredService<EmployerDbContext>();
        var canDbContext = scope.ServiceProvider.GetRequiredService<CandidateDbContext>();
        var jobDbContext = scope.ServiceProvider.GetRequiredService<JobPostingDbContext>();

        await SeedRolesAsync(roleManager, logger);

        await SeedUserAsync(userManager, DevCandidate1UserId, "candidate1@test.ir", "رامین", "احمدی", IdentityRoles.Candidate, logger);
        await SeedUserAsync(userManager, DevEmployer1UserId, "employer1@test.ir", "سیاوش", "مظاهری", IdentityRoles.Employer, logger);
        await SeedUserAsync(userManager, DevCandidate2UserId, "candidate2@test.ir", "لیلا", "حسینی", IdentityRoles.Candidate, logger);
        await SeedUserAsync(userManager, DevEmployer2UserId, "employer2@test.ir", "نگار", "کریمی", IdentityRoles.Employer, logger);

        await SeedReferenceDataAsync(refDbContext, logger);

        await SeedEmployerProfileAsync(empDbContext, Employer1ProfileId, DevEmployer1UserId, "تک‌نوا سولوشن‌ها", "تهران", logger);
        await SeedEmployerProfileAsync(empDbContext, Employer2ProfileId, DevEmployer2UserId, "دیتا‌فلو سیستمز", "اصفهان", logger);
        await empDbContext.SaveChangesAsync();

        await SeedCandidateProfileAsync(canDbContext, Candidate1ProfileId, DevCandidate1UserId, "مهندس نرم‌افزار ارشد", logger);
        await SeedCandidateProfileAsync(canDbContext, Candidate2ProfileId, DevCandidate2UserId, "طراح رابط کاربری", logger);
        await canDbContext.SaveChangesAsync();

        await SeedJobPostingsAsync(jobDbContext, logger);
        await jobDbContext.SaveChangesAsync();

        logger.LogInformation("Development seed data initialised successfully.");
    }

    private static async Task SeedEmployerProfileAsync(
       EmployerDbContext db,
       Guid profileId,
       Guid userId,
       string companyName,
       string industry,
       ILogger logger)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
        INSERT INTO employers.EmployerProfiles
            (Id, UserId, CompanyName, Industry, Province, City, CompanySize, CreatedAt, UpdatedAt)
        VALUES
            (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8)
        """,
            profileId,
            userId,
            companyName,
            industry,
            "تهران",
            "تهران",
            "11-50",          // ← example value for CompanySize (adjust to a valid value in your system)
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        logger.LogInformation(
            "Created dev employer profile: {CompanyName} for user {UserId}",
            companyName,
            userId);
    }



    // ─── Roles ───
    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager, ILogger logger)
    {
        foreach (var roleName in IdentityRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName));
                logger.LogInformation("Created dev role: {Role}", roleName);
            }
        }
    }

    // ─── Users ───

    private static async Task SeedUserAsync(
        UserManager<ApplicationUser> userManager, Guid userId, string email,
        string firstName, string lastName, string role, ILogger logger)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            if (!await userManager.IsInRoleAsync(user, role))
                await userManager.AddToRoleAsync(user, role);
            return;
        }
        var random = new Random();
        string phoneNumber = $"+98912{random.Next(0, 100_000_000):D8}";
        user = new ApplicationUser
        {
            Id = userId,
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
        };

        await userManager.CreateAsync(user, DevPassword);
        await userManager.AddToRoleAsync(user, role);
        logger.LogInformation("Created dev user: {Email} ({Role})", email, role);
    }

    // ─── Reference data (categories + skills) ───

    private static async Task SeedReferenceDataAsync(
  ReferenceDataDbContext db,
  ILogger logger)
    {
        var categories = new[]
        {
(
Id: CategoryItId,
Name: "فناوری اطلاعات",
Description: "دسته‌بندی شغل‌های فناوری اطلاعات"
),
(
Id: CategoryDesignId,
Name: "طراحی و تجربه کاربری",
Description: "دسته‌بندی شغل‌های طراحی"
)
};

        foreach (var category in categories)
        {
            var exists = await db.JobCategories.AnyAsync(c =>
                c.Id == category.Id ||
                c.NormalizedName == category.Name);

            if (!exists)
            {
                db.JobCategories.Add(
                    JobCategory.Create(category.Name, category.Description));
            }
        }

        await db.SaveChangesAsync();

        var skills = new (Guid Id, string Name)[]
        {
                (SkillCSharpId,  "C#"),
                (SkillJsId,      "JavaScript"),
                (SkillAngularId, "Angular"),
                (SkillReactId,   "React"),
                (SkillPythonId,  "Python"),
                (SkillSqlId,     "SQL"),
                (SkillDockerId,  "Docker"),
                (SkillAwsId,     "AWS"),
                (SkillFigmaId,   "Figma"),
                (SkillUxId,      "UX Design")
        };

        foreach (var (id, name) in skills)
        {
            var exists = await db.Skills.AnyAsync(s =>
                s.Id == id ||
                s.NormalizedName == name);

            if (!exists)
            {
                db.Skills.Add(Skill.Create(name, null));
            }
        }

        await db.SaveChangesAsync();

        logger.LogInformation(
            "Reference data seeding completed. Ensured 2 categories and 10 skills exist.");

    }

    // ─── Employer profiles ───



    // ─── Candidate profiles ───

    private static async Task SeedCandidateProfileAsync(
        CandidateDbContext db, Guid profileId, Guid userId, string headline, ILogger logger)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO candidates.CandidateProfiles (Id, UserId, Headline, Biography, City, Province, CreatedAt, UpdatedAt)
            VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7)
            """,
            profileId, userId, headline, null, "تهران", "تهران", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        logger.LogInformation("Created dev candidate profile for user {UserId} ({Headline})", userId, headline);
    }

    // ─── Job postings ───

    private static async Task SeedJobPostingsAsync(JobPostingDbContext db, ILogger logger)
    {
        // Resolve an active city for the required CityId field.
        var city = await db.Set<City>().Where(c => c.IsActive).OrderBy(c => c.Name).FirstOrDefaultAsync();
        if (city == null)
        {
            logger.LogWarning("Skipping job seeding: no active city found.");
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var closingAt = now.AddDays(30);

        var jobs = new (Guid EmployerProfileId, string Title, string Description, Guid CategoryId, string EmploymentType, string WorkArrangement, decimal? SalaryMin, decimal? SalaryMax, Guid[] SkillIds)[]
        {
            (Employer1ProfileId, "Backend Senior Developer - .NET", "Build and maintain transactional systems.", CategoryItId, "FullTime", "OnSite", 100m, 200m, [SkillCSharpId, SkillSqlId, SkillAwsId]),
            (Employer1ProfileId, "Frontend Developer (UI/UX)", "Shape accessible user experiences.", CategoryDesignId, "FullTime", "Remote", 80m, 150m, [SkillJsId, SkillReactId, SkillFigmaId]),
            (Employer2ProfileId, "Python Backend Engineer", "Design and implement data-intensive services.", CategoryItId, "PartTime", "Remote", 70m, 120m, [SkillPythonId, SkillSqlId, SkillDockerId]),
            (Employer2ProfileId, "React Frontend Engineer", "Develop responsive web applications.", CategoryDesignId, "Contract", "OnSite", 60m, 110m, [SkillReactId, SkillJsId, SkillAwsId]),
            (Employer1ProfileId, "DevOps Engineer", "Manage CI/CD and cloud infrastructure.", CategoryItId, "FullTime", "Hybrid", 90m, 160m, [SkillDockerId, SkillAwsId, SkillCSharpId]),
            (Employer2ProfileId, "Product Designer", "Create intuitive product experiences.", CategoryDesignId, "PartTime", "Remote", 50m, 90m, [SkillFigmaId, SkillUxId]),
            (Employer1ProfileId, "Database Administrator", "Optimize and maintain SQL Server databases.", CategoryItId, "FullTime", "OnSite", 85m, 140m, [SkillSqlId, SkillAwsId]),
            (Employer2ProfileId, "Full Stack .NET Developer", "End-to-end feature delivery on the .NET stack.", CategoryItId, "FullTime", "Hybrid", 95m, 170m, [SkillCSharpId, SkillJsId, SkillAngularId]),
            (Employer1ProfileId, "Mobile Developer", "Build native mobile applications.", CategoryItId, "Contract", "Remote", 75m, 130m, [SkillJsId, SkillReactId]),
            (Employer2ProfileId, "Quality Assurance Engineer", "Ensure software quality through testing.", CategoryItId, "PartTime", "OnSite", 55m, 95m, [SkillSqlId, SkillCSharpId]),
        };

        foreach (var (employerId, title, description, categoryId, employmentType, workArrangement, salaryMin, salaryMax, skillIds) in jobs)
        {
            var details = new JobPostingDetails(
                Title: title,
                Description: description,
                CategoryId: categoryId,
                SkillIds: skillIds,
                EmploymentType: ParseEnum<IranJob.Modules.JobPostings.Domain.Enums.EmploymentType>(employmentType),
                WorkArrangement: ParseEnum<IranJob.Modules.JobPostings.Domain.Enums.WorkArrangement>(workArrangement),
                CityId: city.Id,
                SalaryMinimum: salaryMin,
                SalaryMaximum: salaryMax,
                SalaryCurrency: "IRR",
                SalaryPeriod: IranJob.Modules.JobPostings.Domain.Enums.SalaryPeriod.Monthly,
                ClosingAt: closingAt);

            var posting = JobPosting.Create(employerId, details);
            posting.Publish();
            db.JobPostings.Add(posting);
        }

        logger.LogInformation("Seeded {JobCount} published job postings.", jobs.Length);
    }

    private static TEnum ParseEnum<TEnum>(string value) where TEnum : struct, Enum
    {
        return Enum.Parse<TEnum>(value, ignoreCase: true);
    }
}