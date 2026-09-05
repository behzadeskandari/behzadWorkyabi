using IranJob.Modules.Identity.Domain.Entities;

namespace IranJob.Modules.Identity.Application.Abstractions;

public interface IRefreshTokenService
{
    Task<(string RawToken, RefreshToken Entity)> CreateAsync(
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<(RefreshToken Token, string RawToken)?> RotateAsync(
        string rawToken,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(
        string rawToken,
        string? ipAddress,
        string reason,
        CancellationToken cancellationToken = default);

    Task RevokeAllForUserAsync(
        Guid userId,
        string? ipAddress,
        string reason,
        CancellationToken cancellationToken = default);
}
