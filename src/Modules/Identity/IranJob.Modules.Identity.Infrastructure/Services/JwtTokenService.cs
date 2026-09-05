using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ClaimTypes = System.Security.Claims.ClaimTypes;
using System.Text;
using IranJob.Modules.Identity.Application.Abstractions;
using IranJob.Modules.Identity.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace IranJob.Modules.Identity.Infrastructure.Services;

public sealed class JwtTokenService(IOptions<JwtOptions> jwtOptions) : IJwtTokenService
{
    private readonly JwtOptions _options = jwtOptions.Value;

    public string GenerateAccessToken(
        Guid userId,
        string userName,
        IReadOnlyList<string> roles,
        out DateTimeOffset expiresAt)
    {
        expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.AccessTokenExpirationMinutes);
        _ = userName;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (bool IsValid, Guid UserId, IReadOnlyList<string> Roles) ValidateAccessToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            var parameters = CreateValidationParameters();
            var principal = handler.ValidateToken(token, parameters, out _);
            var userIdClaim = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            {
                return (false, Guid.Empty, []);
            }

            var roles = principal.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList();
            return (true, userId, roles);
        }
        catch
        {
            return (false, Guid.Empty, []);
        }
    }

    internal TokenValidationParameters CreateValidationParameters() =>
        new()
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
}
