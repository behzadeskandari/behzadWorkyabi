using System.Security.Cryptography;
using System.Text;
using IranJob.Modules.Identity.Application.Abstractions;
using IranJob.Modules.Identity.Domain.Entities;
using IranJob.Modules.Identity.Infrastructure.Configuration;
using IranJob.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IranJob.Modules.Identity.Infrastructure.Services;

public sealed class RefreshTokenService(
    IdentityDbContext dbContext,
    IOptions<IdentitySecurityOptions> securityOptions,
    IAuthenticationAuditService auditService) : IRefreshTokenService
{
    public async Task<(string RawToken, RefreshToken Entity)> CreateAsync(
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var rawToken = GenerateSecureToken();
        var entity = new RefreshToken
        {
            UserId = userId,
            TokenHash = HashToken(rawToken),
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(securityOptions.Value.RefreshTokenExpirationDays),
            CreatedByIp = ipAddress
        };

        dbContext.RefreshTokens.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (rawToken, entity);
    }

    public async Task<(RefreshToken Token, string RawToken)?> RotateAsync(
        string rawToken,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(rawToken);
        var existing = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (existing is null)
        {
            return null;
        }

        if (existing.IsRevoked)
        {
            await auditService.RecordAsync(
                Domain.Constants.AuthenticationAuditEventTypes.RefreshTokenReuseDetected,
                existing.UserId,
                metadata: $"tokenId={existing.Id}",
                cancellationToken);

            await RevokeDescendantsAsync(existing, ipAddress, "Refresh token reuse detected", cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }

        if (existing.IsExpired)
        {
            existing.RevokedAt = DateTimeOffset.UtcNow;
            existing.RevokedByIp = ipAddress;
            existing.RevocationReason = "Expired";
            await dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }

        var newRawToken = GenerateSecureToken();
        var replacement = new RefreshToken
        {
            UserId = existing.UserId,
            TokenHash = HashToken(newRawToken),
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(securityOptions.Value.RefreshTokenExpirationDays),
            CreatedByIp = ipAddress
        };

        existing.RevokedAt = DateTimeOffset.UtcNow;
        existing.RevokedByIp = ipAddress;
        existing.RevocationReason = "Rotated";
        existing.ReplacedByTokenId = replacement.Id;

        dbContext.RefreshTokens.Add(replacement);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (replacement, newRawToken);
    }

    public async Task<bool> RevokeAsync(
        string rawToken,
        string? ipAddress,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(rawToken);
        var existing = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (existing is null || existing.IsRevoked)
        {
            return false;
        }

        existing.RevokedAt = DateTimeOffset.UtcNow;
        existing.RevokedByIp = ipAddress;
        existing.RevocationReason = reason;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task RevokeAllForUserAsync(
        Guid userId,
        string? ipAddress,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var activeTokens = await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
            token.RevokedByIp = ipAddress;
            token.RevocationReason = reason;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RevokeDescendantsAsync(
        RefreshToken token,
        string? ipAddress,
        string reason,
        CancellationToken cancellationToken)
    {
        if (token.ReplacedByTokenId is null)
        {
            return;
        }

        var descendant = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(item => item.Id == token.ReplacedByTokenId, cancellationToken);

        while (descendant is not null && !descendant.IsRevoked)
        {
            descendant.RevokedAt = DateTimeOffset.UtcNow;
            descendant.RevokedByIp = ipAddress;
            descendant.RevocationReason = reason;

            if (descendant.ReplacedByTokenId is null)
            {
                break;
            }

            descendant = await dbContext.RefreshTokens
                .FirstOrDefaultAsync(item => item.Id == descendant.ReplacedByTokenId, cancellationToken);
        }
    }

    internal static string HashToken(string rawToken)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hashBytes);
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}
