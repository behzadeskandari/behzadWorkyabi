using IranJob.BuildingBlocks.Infrastructure.Events;
using IranJob.Modules.JobPostings.Application.Abstractions;
using IranJob.Modules.Payments.Domain.Events;
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

        await jobPostingService.PublishByIdAsync(jobPostingId.Value, cancellationToken);
        logger.LogInformation(
            "Payment {PaymentId} for job posting {JobId} verified; job posting is published.",
            domainEvent.PaymentId, jobPostingId.Value);
    }
}
