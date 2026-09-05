namespace IranJob.Modules.Identity.Application.Abstractions;

public interface IAuthenticationAuditService
{
    Task RecordAsync(
        string eventType,
        Guid? userId = null,
        string? metadata = null,
        CancellationToken cancellationToken = default);
}
