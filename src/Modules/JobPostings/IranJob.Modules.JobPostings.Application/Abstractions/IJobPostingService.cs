using IranJob.Modules.JobPostings.Application.Contracts;

namespace IranJob.Modules.JobPostings.Application.Abstractions;

public interface IJobPostingService
{
    Task<IReadOnlyList<JobPostingResponse>> GetOwnAsync(Guid userId, CancellationToken cancellationToken);
    Task<JobPostingResponse> GetOwnByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<JobPostingResponse> CreateAsync(Guid userId, CreateJobPostingRequest request, CancellationToken cancellationToken);
    Task<JobPostingResponse> UpdateAsync(Guid userId, Guid id, UpdateJobPostingRequest request, CancellationToken cancellationToken);
    Task<JobPostingResponse> PublishAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<JobPostingResponse> UnpublishAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<JobPostingResponse> CloseAsync(Guid userId, Guid id, CancellationToken cancellationToken);
}
