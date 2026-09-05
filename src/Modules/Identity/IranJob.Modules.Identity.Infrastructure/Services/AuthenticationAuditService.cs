using IranJob.Modules.Identity.Application.Abstractions;
using IranJob.Modules.Identity.Domain.Entities;
using IranJob.Modules.Identity.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace IranJob.Modules.Identity.Infrastructure.Services;

public sealed class AuthenticationAuditService(
    IdentityDbContext dbContext,
    IRequestContext requestContext,
    ILogger<AuthenticationAuditService> logger) : IAuthenticationAuditService
{
    public async Task RecordAsync(
        string eventType,
        Guid? userId = null,
        string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuthenticationAuditEntry
        {
            UserId = userId,
            EventType = eventType,
            Timestamp = DateTimeOffset.UtcNow,
            CorrelationId = requestContext.CorrelationId,
            IpAddress = requestContext.IpAddress,
            UserAgent = requestContext.UserAgent,
            Metadata = metadata
        };

        dbContext.AuthenticationAuditEntries.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Authentication audit {EventType} for user {UserId}",
            eventType,
            userId);
    }
}
