using IranJob.Modules.Payments.Domain.Enums;

namespace IranJob.Modules.Payments.Application.Contracts;

/// <summary>
/// Lightweight payment summary for list views.
/// </summary>
public sealed record PaymentListResponse(
    Guid Id,
    long Amount,
    string Currency,
    string Status,
    string PaymentMethod,
    string Authority,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset? FailedAt,
    string? FailureReason,
    Guid? RelatedJobPostingId,
    Guid EmployerProfileId);

/// <summary>
/// Full payment detail with all available fields.
/// </summary>
public sealed record PaymentDetailResponse(
    Guid Id,
    long Amount,
    string Currency,
    string Status,
    string PaymentMethod,
    string Authority,
    string Description,
    string? CallbackUrl,
    Guid? RelatedJobPostingId,
    Guid EmployerProfileId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset? FailedAt,
    string? FailureReason,
    string PublicationStatus);

/// <summary>
/// Payment summary statistics for Super Admin dashboard.
/// </summary>
public sealed record PaymentSummaryResponse(
    int Total,
    int Successful,
    int Pending,
    int Failed,
    long TotalAmount,
    long SuccessfulAmount);

/// <summary>
/// Paginated list response used by Super Admin payment listing.
/// </summary>
public sealed record PaginatedPaymentResponse(
    IReadOnlyList<PaymentListResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
