using IranJob.Modules.Notifications.Application.Contracts;

namespace IranJob.Modules.Notifications.Application.Abstractions;

/// <summary>
/// Delivers outbound email. Implementations are registered by the Notifications
/// module. When email is not configured (or disabled) the registered
/// implementation no-ops so that callers — e.g. domain-event handlers — never
/// fail an otherwise-successful business operation because email is unavailable.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends <paramref name="message"/> to its single recipient. Implementations
    /// must not throw when email is disabled; transport failures after an
    /// enabled send are logged and swallowed so a notification problem cannot
    /// roll back the business transaction that triggered it.
    /// </summary>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
