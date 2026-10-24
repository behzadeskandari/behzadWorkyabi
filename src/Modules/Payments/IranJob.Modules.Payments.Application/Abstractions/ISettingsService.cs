using IranJob.Modules.Payments.Domain.Settings;

namespace IranJob.Modules.Payments.Application.Abstractions;

/// <summary>
/// Service for reading and updating persisted, non-secret application settings.
/// Encourages explicit, type-safe updates rather than arbitrary key writes.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Returns all stored settings as key-value pairs. Keys are setting names;
    /// values are the persisted strings. Secrets are never returned.
    /// </summary>
    Task<Dictionary<string, string>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the payment fee (amount and currency). The amount is in the
    /// smallest currency unit (rial for IRR). A value of 0 disables paid publishing.
    /// </summary>
    Task<(long Amount, string Currency)> GetPaymentFeeAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Updates the payment fee. Only future eligible transactions are affected;
    /// existing persisted payment records are never modified.
    /// </summary>
    Task<(long Amount, string Currency)> UpdatePaymentFeeAsync(long amount, string currency, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the current email notification configuration.
    /// </summary>
    Task<(bool Enabled, string FromAddress)> GetEmailNotificationsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Updates email notification configuration.
    /// </summary>
    Task UpdateEmailNotificationsAsync(bool enabled, string fromAddress, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the application display name and version.
    /// </summary>
    Task<(string Name, string Version)> GetAppInfoAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Updates the application display name and version (read-only UI).
    /// </summary>
    Task UpdateAppInfoAsync(string name, string version, CancellationToken cancellationToken);
}
