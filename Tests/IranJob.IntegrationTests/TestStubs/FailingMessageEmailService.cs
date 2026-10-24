using IranJob.Modules.Notifications.Application.Abstractions;
using IranJob.Modules.Notifications.Application.Contracts;

namespace IranJob.IntegrationTests.TestStubs;

/// <summary>Simulates a mail transport failure for message mirroring only.</summary>
internal sealed class FailingMessageEmailService : IEmailService
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (message.Subject.StartsWith("New message in conversation ", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Simulated message email delivery failure.");
        }

        return Task.CompletedTask;
    }
}
