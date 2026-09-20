using IranJob.Modules.Candidates.Domain.Enums;
using IranJob.SharedKernel.Entities;

namespace IranJob.Modules.Candidates.Domain.Entities;

public sealed class CandidateProfile : Entity
{
    public Guid UserId { get; set; }

    public string? Headline { get; set; }

    public string? Biography { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public Gender? Gender { get; set; }

    public string? City { get; set; }

    public string? Province { get; set; }

    //public string? Phone { get; set; }

    //public string? Email { get; set; }

    public string? LinkedInUrl { get; set; }

    public string? GitHubUrl { get; set; }

    public string? PortfolioUrl { get; set; }

    public decimal? ExpectedSalary { get; set; }

    public SalaryType? SalaryType { get; set; }

    public EmploymentStatus? EmploymentStatus { get; set; }

    public Availability? Availability { get; set; }

    public MilitaryStatus? MilitaryStatus { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
