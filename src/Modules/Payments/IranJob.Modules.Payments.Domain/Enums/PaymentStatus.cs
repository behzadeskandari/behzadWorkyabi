namespace IranJob.Modules.Payments.Domain.Enums;

/// <summary>
/// Lifecycle state of a payment within the platform.
/// </summary>
public enum PaymentStatus
{
    Pending = 1,
    Paid = 2,
    Failed = 3,
    Expired = 4
}
