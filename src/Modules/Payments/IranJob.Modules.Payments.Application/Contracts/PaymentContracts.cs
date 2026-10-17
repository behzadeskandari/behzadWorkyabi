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

/// <summary>Request body for a generic employer-initiated payment (e.g. future credit top-up).</summary>
public sealed record CreatePaymentRequest(
    Guid? JobPostingId,
    long Amount,
    string Currency,
    string? Description,
    string? CallbackUrl);

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
    Guid? JobPostingId,
    string? Message);

/// <summary>Public read model for a payment record.</summary>
public sealed record PaymentResponse(
    Guid Id,
    Guid EmployerProfileId,
    long Amount,
    string Currency,
    string Status,
    string PaymentMethod,
    string Authority,
    Guid? JobPostingId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt,
    string? FailureReason);
