using IranJob.BuildingBlocks.Infrastructure.Events;
using IranJob.Modules.Candidates.Infrastructure.Persistence;
using IranJob.Modules.JobPostings.Domain.Events;
using IranJob.Modules.JobPostings.Infrastructure.Persistence;
using IranJob.Modules.Notifications.Application.Abstractions;
using IranJob.Modules.Notifications.Application.Contracts;
using IranJob.Modules.Notifications.Domain.Enums;
using EmployerProfileEntity = IranJob.Modules.EmployerProfile.Domain.Entities.EmployerProfile;
using IranJob.SharedKernel.Abstractions;
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
    IEmailService emailService,
    IUserEmailResolver userEmailResolver,
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
            title!,
            relatedResourceId,
            cancellationToken);

        // Best-effort email mirror of the in-app notification. Failures are logged and
        // swallowed (inside IEmailService) so they never affect the business operation.
        await SendEmailMirrorAsync(recipientUserId, type.Value, title!, cancellationToken);
    }

    /// <summary>
    /// Resolves the recipient's email and sends a plain-text mirror of the in-app
    /// notification. This is best-effort: a missing email or transport failure is
    /// logged and ignored so the application transition (already committed) is
    /// never affected.
    /// </summary>
    private async Task SendEmailMirrorAsync(
        Guid recipientUserId,
        NotificationType type,
        string title,
        CancellationToken cancellationToken)
    {
        try
        {
            var recipientEmail = await userEmailResolver.ResolveEmailAsync(recipientUserId, cancellationToken);
            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                logger.LogDebug(
                    "No email address for user {User}; skipping email mirror for {Type}.",
                    recipientUserId, type);
                return;
            }

            await emailService.SendAsync(
                new EmailMessage(
                    To: recipientEmail,
                    Subject: $"IranJob — {title}",
                    Body: $"{title}\n\nبرای مشاهده جزئیات وارد حساب کاربری خود در ایران‌جاب شوید."),
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to send email mirror for user {User} transition {Type}.",
                recipientUserId, type);
        }
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
        // Resolve the candidate profile id first via the job-postings context, then
        // the owning user id via the candidates context. These MUST be two separate
        // queries: composing a single LINQ query across two different DbContext
        // instances throws "Cannot use multiple context instances within a single
        // query execution".
        var candidateProfileId = await jobDbContext.JobApplications.AsNoTracking()
            .Where(app => app.Id == @event.ApplicationId)
            .Select(app => app.CandidateProfileId)
            .FirstOrDefaultAsync(ct);

        if (candidateProfileId == Guid.Empty)
        {
            logger.LogWarning("Could not resolve candidate profile for application {Application}.", @event.ApplicationId);
            return (null, Guid.Empty, null, null);
        }

        var candidateUserId = await candidateDbContext.CandidateProfiles.AsNoTracking()
            .Where(profile => profile.Id == candidateProfileId)
            .Select(profile => profile.UserId)
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
