namespace IranJob.Modules.JobPostings.Application.Contracts;

public sealed record SavedJobItem(Guid JobPostingId, string Title, string CompanyName, string Category, string City, DateTimeOffset SavedAt, bool IsAvailable);
public sealed record SavedJobPage(IReadOnlyList<SavedJobItem> Items, int Page, int PageSize, int TotalCount);
public sealed record JobAlertRequest(string Name, Guid? CategoryId, Guid? CityId, string? WorkArrangement, IReadOnlyList<Guid> SkillIds, bool IsEnabled = true);
public sealed record JobAlertDto(Guid Id, string Name, Guid? CategoryId, Guid? CityId, string? WorkArrangement, IReadOnlyList<Guid> SkillIds, bool IsEnabled, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public interface ICandidateJobPreferenceService
{
    Task<bool> SaveJobAsync(Guid userId, Guid jobId, CancellationToken ct);
    Task<bool> RemoveSavedJobAsync(Guid userId, Guid jobId, CancellationToken ct);
    Task<SavedJobPage> ListSavedJobsAsync(Guid userId, int page, int pageSize, CancellationToken ct);
    Task<IReadOnlyList<Guid>> GetSavedStatusesAsync(Guid userId, IReadOnlyCollection<Guid> jobIds, CancellationToken ct);
    Task<IReadOnlyList<JobAlertDto>> ListAlertsAsync(Guid userId, CancellationToken ct);
    Task<JobAlertDto> CreateAlertAsync(Guid userId, JobAlertRequest request, CancellationToken ct);
    Task<JobAlertDto> UpdateAlertAsync(Guid userId, Guid alertId, JobAlertRequest request, CancellationToken ct);
    Task DeleteAlertAsync(Guid userId, Guid alertId, CancellationToken ct);
}

public interface IJobAlertMatcher
{
    Task NotifyMatchesAsync(Guid jobId, CancellationToken ct);
}
