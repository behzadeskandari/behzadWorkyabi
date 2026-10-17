namespace IranJob.Modules.JobPostings.Application.Contracts;

/// <summary>
/// Single row in the administrative job listing.
/// Includes employer and reference-data fields for display.
/// </summary>
public sealed record AdminJobListItemResponse(
    Guid Id,
    string Title,
    Guid CategoryId,
    string CategoryName,
    string EmploymentType,
    string WorkArrangement,
    string? CityName,
    string? ProvinceName,
    string? CountryName,
    string CompanyName,
    Guid EmployerUserId,
    decimal? SalaryMinimum,
    decimal? SalaryMaximum,
    string? SalaryCurrency,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? ClosedAt,
    Guid? ModeratedByUserId,
    string? ModerationReason,
    DateTimeOffset? ModeratedAt);

/// <summary>
/// Paginated result for the administrative job listing.
/// </summary>
public sealed record AdminJobPageResult(
    IReadOnlyList<AdminJobListItemResponse> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

/// <summary>
/// Detailed view of a single job posting for administrators.
/// </summary>
public sealed record AdminJobDetailResponse(
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
    DateTimeOffset? ClosingAt,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? ClosedAt,
    Guid EmployerUserId,
    string CompanyName,
    string Industry,
    Guid? ModeratedByUserId,
    string? ModerationReason,
    DateTimeOffset? ModeratedAt);

/// <summary>
/// Request body for a moderation action. The reason is optional.
/// </summary>
public sealed record ModerateJobRequest(string? Reason = null);