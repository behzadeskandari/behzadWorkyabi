using IranJob.BuildingBlocks.Infrastructure.Events;
using IranJob.Modules.Candidates.Infrastructure.Persistence;
using IranJob.Modules.JobPostings.Domain.Events;
using IranJob.Modules.JobPostings.Infrastructure.Persistence;
using IranJob.Modules.Notifications.Application.Abstractions;
using IranJob.Modules.Notifications.Domain.Enums;
using EmployerProfileEntity = IranJob.Modules.EmployerProfile.Domain.Entities.EmployerProfile;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IranJob.Modules.JobPostings.Infrastructure.Services;

/// <summary>
/// Handles <see cref="JobApplicationStatusChanged"/> domain events raised by
/// <see cref="JobApplicationService"/> after an application transition is
/// committed. Resolves the recipient user id (employer for candidate actions,
/// candidate for employer actions) from the owning module DbContexts and
/// persists a single, idempotent notification via
/// <see cref="INotificationService"/>.
/// </summary>
public sealed class JobApplicationNotificationHandler(
    JobPostingDbContext jobDbContext,
    CandidateDbContext candidateDbContext,
    INotificationService notificationService,
    ILogger<JobApplicationNotificationHandler> logger)
    : IEventHandler<JobApplicationStatusChanged>
{
    public async Task HandleAsync(JobApplicationStatusChanged domainEvent, CancellationToken cancellationToken)
    {
        try
        {
            await CreateNotificationForTransitionAsync(domainEvent, cancellationToken);
        }
        catch (Exception exception)
        {
            // A notification failure must not roll back the already-committed application
            // transition (event dispatch happens post-commit). Logged for observability;
            // the business operation itself remains consistent.
            logger.LogError(
                exception,
                "Failed to persist notification for application {Application} transition {Transition}.",
                domainEvent.ApplicationId, domainEvent.Transition);
        }
    }

    private async Task CreateNotificationForTransitionAsync(
        JobApplicationStatusChanged domainEvent,
        CancellationToken cancellationToken)
    {
        var (type, recipientUserId, title, relatedResourceId) = domainEvent.Transition switch
        {
            JobApplicationTransition.Applied => await ResolveEmployerAsync(
                domainEvent, "کارجوی جدید برای آگهی شغلی شما", cancellationToken),

            JobApplicationTransition.Withdrawn => await ResolveEmployerAsync(
                domainEvent, "درخواست شما بازگشت یافت", cancellationToken),

            JobApplicationTransition.Reviewed => await ResolveCandidateAsync(
                domainEvent, "درخواست شما مورد بررسی قرار گرفت", cancellationToken),

            JobApplicationTransition.Accepted => await ResolveCandidateAsync(
                domainEvent, "درخواست شما پذیرفته شد", cancellationToken),

            JobApplicationTransition.Rejected => await ResolveCandidateAsync(
                domainEvent, "درخواست شما رد شد", cancellationToken),

            _ => ((NotificationType?)null, Guid.Empty, (string?)null, (Guid?)null)
        };

        if (type is null || recipientUserId == Guid.Empty)
        {
            logger.LogWarning(
                "No notification produced for application {Application} transition {Transition}.",
                domainEvent.ApplicationId, domainEvent.Transition);
            return;
        }
        // Idempotency: a retry / repeated dispatch is a no-op.
        await notificationService.CreateIfNotExistsAsync(
            recipientUserId,
            type.Value,
            title!,
            relatedResourceId,
            cancellationToken);
    }

    private async Task<(NotificationType? type, Guid recipientUserId, string? title, Guid? relatedResourceId)>
        ResolveEmployerAsync(JobApplicationStatusChanged @event, string title, CancellationToken ct)
    {
        // Resolve the job owner's employer profile id, then its user id.
        var employerProfileId = await (
            from app in jobDbContext.JobApplications.AsNoTracking()
            join job in jobDbContext.JobPostings.AsNoTracking() on app.JobPostingId equals job.Id
            where app.Id == @event.ApplicationId
            select job.EmployerProfileId)
            .FirstOrDefaultAsync(ct);

        if (employerProfileId == Guid.Empty)
        {
            logger.LogWarning("Could not resolve employer profile for application {Application}.", @event.ApplicationId);
            return (null, Guid.Empty, null, null);
        }

        var employerUserId = await jobDbContext.Set<EmployerProfileEntity>()
            .AsNoTracking()
            .Where(profile => profile.Id == employerProfileId)
            .Select(profile => profile.UserId)
            .FirstOrDefaultAsync(ct);

        var type = @event.Transition == JobApplicationTransition.Applied
            ? NotificationType.ApplicationSubmitted
            : NotificationType.ApplicationWithdrawn;

        return (type, employerUserId, title, @event.ApplicationId);
    }

    private async Task<(NotificationType? type, Guid recipientUserId, string? title, Guid? relatedResourceId)>
        ResolveCandidateAsync(JobApplicationStatusChanged @event, string title, CancellationToken ct)
    {
        // Resolve the candidate profile owner's user id via the candidate module context.
        var candidateUserId = await (
            from app in jobDbContext.JobApplications.AsNoTracking()
            join profile in candidateDbContext.CandidateProfiles.AsNoTracking()
                on app.CandidateProfileId equals profile.Id
            where app.Id == @event.ApplicationId
            select profile.UserId)
            .FirstOrDefaultAsync(ct);

        if (candidateUserId == Guid.Empty)
        {
            logger.LogWarning("Could not resolve candidate user for application {Application}.", @event.ApplicationId);
            return (null, Guid.Empty, null, null);
        }

        var type = @event.Transition switch
        {
            JobApplicationTransition.Reviewed => NotificationType.ApplicationReviewed,
            JobApplicationTransition.Accepted => NotificationType.ApplicationAccepted,
            JobApplicationTransition.Rejected => NotificationType.ApplicationRejected,
            _ => (NotificationType?)null
        };

        return (type, candidateUserId, title, @event.ApplicationId);
    }
}
