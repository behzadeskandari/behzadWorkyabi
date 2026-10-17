using IranJob.SharedKernel.Events;

namespace IranJob.Modules.Payments.Domain.Events;

/// <summary>
/// Raised after a previously Pending payment is confirmed (ZarinPal verification
/// returned success). <see cref="JobPostingId"/> is set when the payment is tied
/// to a job posting, so the JobPostings module can publish the listing.
/// </summary>
public sealed record PaymentCompleted(
    Guid PaymentId,
    Guid EmployerProfileId,
    Guid? JobPostingId,
    string? ReferenceId = null) : IDomainEvent;
