using IranJob.Modules.JobPostings.Domain.Entities;
using IranJob.SharedKernel.Events;

namespace IranJob.Modules.JobPostings.Domain.Events;

/// <summary>
/// Raised after a job-application lifecycle transition is persisted.
/// The transition is the only payload needed — recipients and messages are
/// resolved by the subscribed <see cref="IEventHandler{TEvent}"/>, keeping
/// the domain module decoupled from the Notifications module.
/// </summary>
public sealed record JobApplicationStatusChanged(
    Guid ApplicationId,
    Guid CandidateProfileId,
    Guid JobPostingId,
    JobApplicationTransition Transition) : IDomainEvent
{
    public static JobApplicationStatusChanged Create(
        JobApplication application,
        JobApplicationTransition transition) =>
        new(application.Id, application.CandidateProfileId, application.JobPostingId, transition);
}
