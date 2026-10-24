namespace IranJob.Modules.Payments.Application.Contracts;

/// <summary>
/// Lightweight payment summary exposed in job-publishing responses so other
/// modules never depend on the <see cref="Domain.Entities.Payment"/> aggregate.
/// </summary>
public sealed record PaymentInfo(
    Guid PaymentId,
    string Authority,
    string RedirectUrl,
    long Amount,
    string Currency,
    string Status,
    string PaymentMethod,
    bool Completed)
{

}

/// <summary>Result returned after a payment (job-publish fee) is initiated with the gateway.</summary>
public sealed record PaymentInitiationResponse(
    Guid PaymentId,
    string Authority,
    string RedirectUrl,
    long Amount,
    string Currency,
    string Status,
    string PaymentMethod,
    Guid? JobPostingId,
    bool IsCompleted);

/// <summary>Result returned after the ZarinPal callback is verified.</summary>
public sealed record PaymentVerifyResponse(
    Guid PaymentId,
    string Status,
    string PublicationStatus,
    Guid? JobPostingId,
    string? Message);

/// <summary>Public read model for a payment record.</summary>
public sealed record PaymentResponse(
    Guid Id,
    Guid EmployerProfileId,
    long Amount,
    string Currency,
    string Status,
    string PublicationStatus,
    string PaymentMethod,
    string Authority,
    Guid? JobPostingId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt,
    string? FailureReason);
