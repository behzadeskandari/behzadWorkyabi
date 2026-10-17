using IranJob.SharedKernel.Events;

namespace IranJob.Modules.Payments.Domain.Events;

/// <summary>
/// Raised when a payment transitions to the Failed (or Expired) state.
/// </summary>
public sealed record PaymentFailed(
    Guid PaymentId,
    Guid EmployerProfileId,
    Guid? JobPostingId,
    string? Reason = null) : IDomainEvent;
