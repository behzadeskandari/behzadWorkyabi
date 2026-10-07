namespace IranJob.Modules.JobPostings.Application.Contracts;

public abstract record JobPostingInput
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public IReadOnlyList<Guid> SkillIds { get; init; } = Array.Empty<Guid>();
    public string EmploymentType { get; init; } = string.Empty;
    public string WorkArrangement { get; init; } = string.Empty;
    public Guid? CityId { get; init; }
    public decimal? SalaryMinimum { get; init; }
    public decimal? SalaryMaximum { get; init; }
    public string? SalaryCurrency { get; init; }
    public string? SalaryPeriod { get; init; }
    public DateTimeOffset? ClosingAt { get; init; }
}

public sealed record CreateJobPostingRequest : JobPostingInput;
public sealed record UpdateJobPostingRequest : JobPostingInput;

public sealed record JobPostingReference(Guid Id, string Name);

public sealed record JobPostingResponse(
    Guid Id,
    string Title,
    string Description,
    Guid CategoryId,
    string CategoryName,
    IReadOnlyList<JobPostingReference> Skills,
    string EmploymentType,
    string WorkArrangement,
    Guid? CityId,
    string? CityName,
    string? ProvinceName,
    string? CountryName,
    decimal? SalaryMinimum,
    decimal? SalaryMaximum,
    string? SalaryCurrency,
    string? SalaryPeriod,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? ClosedAt,
    DateTimeOffset? ClosingAt);
