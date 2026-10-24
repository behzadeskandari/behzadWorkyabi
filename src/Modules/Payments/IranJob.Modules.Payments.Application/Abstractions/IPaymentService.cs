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
    /// Verifies a ZarinPal callback by <see cref="authority"/> and, on success,
    /// marks the payment Paid and dispatches <see cref="Events.PaymentCompleted"/>.
    /// </summary>
    Task<PaymentVerifyResponse> VerifyByCallbackAsync(string authority, string zarinStatus, CancellationToken cancellationToken);

    Task<PaymentResponse?> GetForEmployerAsync(Guid paymentId, Guid employerProfileId, CancellationToken cancellationToken);

    Task<bool> HasCompletedPaymentForJobAsync(Guid jobPostingId, CancellationToken cancellationToken);

    /// <summary>
    /// Super Admin: returns a paginated, filterable list of all payments.
    /// </summary>
    Task<(IReadOnlyList<PaymentListResponse> Items, int TotalCount)> GetAllPaymentsAsync(
        int page,
        int pageSize,
        string? search,
        string? status,
        string? paymentMethod,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        CancellationToken cancellationToken);

    /// <summary>
    /// Super Admin: returns payment summary statistics.
    /// </summary>
    Task<PaymentSummaryResponse> GetPaymentSummaryAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Super Admin: returns full detail for a single payment by ID.
    /// </summary>
    Task<PaymentDetailResponse?> GetPaymentDetailAsync(Guid paymentId, CancellationToken cancellationToken);
}
