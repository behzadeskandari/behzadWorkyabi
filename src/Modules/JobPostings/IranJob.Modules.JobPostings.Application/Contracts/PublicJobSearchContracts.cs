namespace IranJob.Modules.JobPostings.Application.Contracts;

public sealed class PublicJobSearchQuery
{
    public string? Q { get; set; }
    public Guid? CategoryId { get; set; }
    public List<Guid> SkillIds { get; set; } = [];
    public Guid? CountryId { get; set; }
    public Guid? ProvinceId { get; set; }
    public Guid? CityId { get; set; }
    public string? WorkArrangement { get; set; }
    public string? EmploymentType { get; set; }
    public decimal? SalaryMinimum { get; set; }
    public decimal? SalaryMaximum { get; set; }
    public string? SalaryCurrency { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string Sort { get; set; } = "Newest";
}

public sealed record PublicJobSearchPage(
    IReadOnlyList<PublicJobSearchItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record PublicJobSearchItem(
    Guid Id,
    string Title,
    string CompanyName,
    string? CompanyLogoUrl,
    string Category,
    string Description,
    IReadOnlyList<string> Skills,
    string City,
    string Province,
    string Country,
    string EmploymentType,
    string WorkArrangement,
    decimal? SalaryMinimum,
    decimal? SalaryMaximum,
    string? SalaryCurrency,
    string? SalaryPeriod,
    DateTimeOffset PublishedAt);

public sealed record PublicEmployerInformation(
    string CompanyName,
    string? Description,
    string Industry,
    string? WebsiteUrl,
    string? LogoUrl);

public sealed record PublicJobDetails(
    Guid Id,
    string Title,
    string Description,
    PublicEmployerInformation Employer,
    string Category,
    IReadOnlyList<string> Skills,
    string City,
    string Province,
    string Country,
    string EmploymentType,
    string WorkArrangement,
    decimal? SalaryMinimum,
    decimal? SalaryMaximum,
    string? SalaryCurrency,
    string? SalaryPeriod,
    DateTimeOffset PublishedAt);
