using IranJob.Modules.JobPostings.Application.Contracts;

namespace IranJob.Modules.JobPostings.Application.Abstractions;

/// <summary>
/// Administrative moderation of job postings.
/// All methods are protected by the Admin authorization policy at the controller level.
/// Moderation actions validate legal state transitions within the domain layer.
/// </summary>
public interface IAdminJobModerationService
{
    Task<AdminJobPageResult> GetJobsAsync(
        int page,
        int pageSize,
        string? search = null,
        string? status = null,
        Guid? categoryId = null,
        Guid? cityId = null,
        string? employerName = null,
        string sort = "Newest",
        CancellationToken cancellationToken = default);

    Task<AdminJobDetailResponse> GetJobByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<AdminJobDetailResponse> ModerateUnpublishAsync(Guid id, Guid actorUserId, string? reason, CancellationToken cancellationToken);

    Task<AdminJobDetailResponse> ModeratePublishAsync(Guid id, Guid actorUserId, string? reason, CancellationToken cancellationToken);

    Task<AdminJobDetailResponse> ModerateCloseAsync(Guid id, Guid actorUserId, string? reason, CancellationToken cancellationToken);
}