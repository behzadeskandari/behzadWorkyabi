namespace IranJob.Modules.Identity.Application.Abstractions;

public interface IRequestContext
{
    string? IpAddress { get; }

    string? UserAgent { get; }

    string? CorrelationId { get; }
}
