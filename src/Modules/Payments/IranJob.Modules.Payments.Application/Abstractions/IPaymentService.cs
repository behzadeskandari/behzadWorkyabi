using IranJob.Modules.Payments.Application.Contracts;

namespace IranJob.Modules.Payments.Application.Abstractions;

/// <summary>
/// Orchestrates job-publish payment lifecycle: fee initiation (ZarinPal),
/// callback verification, and emits domain events on terminal states.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Initiates (or free-completes) the publishing fee for a job posting.
    /// </summary>
    Task<PaymentInitiationResponse> InitiateForJobPublishAsync(
        Guid employerProfileId,
        Guid jobPostingId,
        CancellationToken cancellationToken);

        /// <summary>
    /// Initiates a generic employer payment (resolved from the caller's user id).
    /// The caller (Presentation layer) supplies the resolved employer profile id.
    /// </summary>
    Task<PaymentInitiationResponse> InitiateAsync(
        Guid employerProfileId,
        CreatePaymentRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Verifies a ZarinPal callback by <see cref="authority"/> and, on success,
    /// marks the payment Paid and dispatches <see cref="Events.PaymentCompleted"/>.
    /// </summary>
    Task<PaymentVerifyResponse> VerifyByCallbackAsync(string authority, string zarinStatus, Guid employerProfileId, CancellationToken cancellationToken);

    Task<PaymentResponse?> GetAsync(Guid paymentId, CancellationToken cancellationToken);

    Task<bool> HasCompletedPaymentForJobAsync(Guid jobPostingId, CancellationToken cancellationToken);
}
