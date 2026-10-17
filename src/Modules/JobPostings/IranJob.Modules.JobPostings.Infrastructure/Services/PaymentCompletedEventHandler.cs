using IranJob.BuildingBlocks.Infrastructure.Events;
using IranJob.Modules.JobPostings.Application.Abstractions;
using IranJob.Modules.Payments.Domain.Events;
using IranJob.SharedKernel.Exceptions;
using Microsoft.Extensions.Logging;

namespace IranJob.Modules.JobPostings.Infrastructure.Services;

/// <summary>
/// Handles the <see cref="PaymentCompleted"/> domain event raised by the Payments
/// module after a publish-fee payment is verified. Publishes the associated job
/// posting, completing the fee-gated job-publishing flow.
/// </summary>
public sealed class PaymentCompletedEventHandler(
    IJobPostingService jobPostingService,
    ILogger<PaymentCompletedEventHandler> logger)
    : IEventHandler<PaymentCompleted>
{
    public async Task HandleAsync(PaymentCompleted domainEvent, CancellationToken cancellationToken)
    {
        var jobPostingId = domainEvent.JobPostingId;
        if (jobPostingId is null)
        {
            logger.LogInformation(
                "Payment {PaymentId} completed without an associated job posting; nothing to publish.",
                domainEvent.PaymentId);
            return;
        }

        try
        {
            var response = await jobPostingService.PublishByIdAsync(jobPostingId.Value, cancellationToken);
            logger.LogInformation(
                "Payment {PaymentId} for job posting {JobId} verified; job posting published.",
                domainEvent.PaymentId, jobPostingId.Value);
        }
        catch (Exception exception) when (exception is DomainException or NotFoundException)
        {
            // The job could already be published, closed, or removed between
            // payment initiation and verification. These are expected race or
            // lifecycle conditions, not critical errors.
            logger.LogWarning(
                exception,
                "Payment {PaymentId} completed for job posting {JobId} but publishing was skipped: {Message}",
                domainEvent.PaymentId, jobPostingId.Value, exception.Message);
        }
    }
}
