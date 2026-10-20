using System.Net;
using System.Net.Mail;
using IranJob.BuildingBlocks.Infrastructure.Configuration;
using IranJob.Modules.Notifications.Application.Abstractions;
using IranJob.Modules.Notifications.Application.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IranJob.Modules.Notifications.Infrastructure.Services;

/// <summary>
/// Sends email over SMTP using the framework's built-in <see cref="SmtpClient"/>
/// (no third-party dependency). Email is opt-in via <see cref="EmailOptions.Enabled"/>;
/// when disabled this service logs and returns without touching the network, so
/// environments without an SMTP server (tests, local dev) are unaffected.
/// Transport failures are logged and swallowed so an email problem can never
/// roll back the business operation that triggered the notification.
/// </summary>
public sealed class SmtpEmailService(
    IOptions<EmailOptions> options,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly EmailOptions options = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!options.Enabled)
        {
            logger.LogDebug(
                "Email disabled; skipping message to {Recipient} with subject {Subject}.",
                message.To, message.Subject);
            return;
        }

        if (string.IsNullOrWhiteSpace(options.Host) || string.IsNullOrWhiteSpace(options.FromAddress))
        {
            logger.LogWarning(
                "Email enabled but Host/FromAddress missing; skipping message to {Recipient}.",
                message.To);
            return;
        }

        try
        {
            using var smtpClient = CreateSmtpClient();
            using var mailMessage = BuildMailMessage(message);

            // SmtpClient.SendMailAsync has no CancellationToken overload on .NET 8,
            // so the token is observed only around the awaited task boundary.
            await smtpClient.SendMailAsync(mailMessage).ConfigureAwait(false);

            logger.LogInformation(
                "Sent email to {Recipient} with subject {Subject}.", message.To, message.Subject);
        }
        catch (Exception exception)
        {
            // Never propagate: the originating business transaction already committed.
            logger.LogError(
                exception,
                "Failed to send email to {Recipient} with subject {Subject}.",
                message.To, message.Subject);
        }
    }

    private SmtpClient CreateSmtpClient()
    {
        var client = new SmtpClient(options.Host, options.Port)
        {
            EnableSsl = options.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrWhiteSpace(options.UserName))
        {
            client.Credentials = new NetworkCredential(options.UserName, options.Password);
        }

        return client;
    }

    private MailMessage BuildMailMessage(EmailMessage message)
    {
        var mailMessage = new MailMessage
        {
            From = new MailAddress(options.FromAddress, options.FromDisplayName),
            Subject = message.Subject,
            Body = message.Body,
            IsBodyHtml = message.IsHtml
        };
        mailMessage.To.Add(message.To);
        return mailMessage;
    }
}
