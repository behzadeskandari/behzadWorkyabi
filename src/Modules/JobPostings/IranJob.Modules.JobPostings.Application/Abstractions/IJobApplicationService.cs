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

    /// <summary>
    /// Returns the candidate's CV file when the employer has an authorized
    /// job application for the same job posting.
    /// </summary>
            Task<EmployerCvAccessResponse> DownloadCandidateCvAsync(
        Guid employerUserId,
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns the candidate's structured resume view for an employer who owns
    /// the job application. The resume must have reached the <c>ReadyToShare</c>
    /// consent state; otherwise a <see cref="SharedKernel.Exceptions.NotFoundException"/>
    /// is thrown (404) to avoid leaking the existence of a non-consented resume.
    /// </summary>
    Task<EmployerResumeViewResponse> GetCandidateResumeAsync(
        Guid employerUserId,
        Guid applicationId,
        CancellationToken cancellationToken);
}
