using IranJob.Modules.JobPostings.Application.Contracts;

namespace IranJob.Modules.JobPostings.Application.Abstractions;

public interface IJobApplicationService
{
    Task<JobApplicationResponse> ApplyAsync(Guid userId, Guid jobId, CancellationToken cancellationToken);
    Task<IReadOnlyList<JobApplicationResponse>> GetOwnAsync(Guid userId, CancellationToken cancellationToken);
    Task<JobApplicationResponse> GetOwnByIdAsync(Guid userId, Guid applicationId, CancellationToken cancellationToken);
    Task<JobApplicationResponse> WithdrawAsync(Guid userId, Guid applicationId, CancellationToken cancellationToken);

    Task<IReadOnlyList<EmployerApplicationListResponse>> GetEmployerApplicationsAsync(Guid employerUserId, CancellationToken cancellationToken);
    Task<EmployerApplicationResponse> GetEmployerApplicationByIdAsync(Guid employerUserId, Guid applicationId, CancellationToken cancellationToken);
    Task<EmployerApplicationResponse> ReviewApplicationAsync(Guid employerUserId, Guid applicationId, CancellationToken cancellationToken);
    Task<EmployerApplicationResponse> AcceptApplicationAsync(Guid employerUserId, Guid applicationId, CancellationToken cancellationToken);
    Task<EmployerApplicationResponse> RejectApplicationAsync(Guid employerUserId, Guid applicationId, CancellationToken cancellationToken);
}
