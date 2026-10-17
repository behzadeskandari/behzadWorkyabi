namespace IranJob.Modules.Payments.Application.Contracts;

/// <summary>
/// Request payload sent to ZarinPal v4 (WebGate) PaymentRequest endpoint.
/// </summary>
public sealed record ZarinPalRequest(
    string MerchantId,
    long Amount,
    string Description,
    string CallbackUrl,
    string Currency,
    IDictionary<string, string>? Metadata = null);

/// <summary>
/// Response from ZarinPal PaymentRequest. <see cref="Code"/> 100 means a
/// usable <see cref="Authority"/> was issued.
/// </summary>
public sealed record ZarinPalRequestResult(int Code, string Message, string Authority);

/// <summary>
/// Request payload sent to ZarinPal v4 PaymentVerification endpoint.
/// </summary>
public sealed record ZarinPalVerifyRequest(
    string MerchantId,
    long Amount,
    string Authority,
    string Currency = "IRR");

/// <summary>
/// Response from ZarinPal PaymentVerification. <see cref="Code"/> 100 means
/// verified; 101 means already verified (idempotent success).
/// </summary>
public sealed record ZarinPalVerifyResult(int Code, string Message, string? ReferenceId);
