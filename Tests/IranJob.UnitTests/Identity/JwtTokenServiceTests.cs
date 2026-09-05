using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using IranJob.Modules.Identity.Infrastructure.Configuration;
using IranJob.Modules.Identity.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace IranJob.UnitTests.Identity;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService(
        string issuer = "IranJob",
        string audience = "IranJob.Client",
        string secret = "UNIT_TEST_SECRET_KEY_MUST_BE_LONG_ENOUGH_32",
        int minutes = 15)
    {
        return new JwtTokenService(Options.Create(new JwtOptions
        {
            Issuer = issuer,
            Audience = audience,
            SecretKey = secret,
            AccessTokenExpirationMinutes = minutes
        }));
    }

    [Fact]
    public void ValidToken_IsAccepted()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();
        var token = service.GenerateAccessToken(userId, "unused", ["Candidate"], out var expiresAt);

        var result = service.ValidateAccessToken(token);

        result.IsValid.Should().BeTrue();
        result.UserId.Should().Be(userId);
        result.Roles.Should().Contain("Candidate");
        expiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void ExpiredToken_IsRejected()
    {
        var service = CreateService(minutes: -5);
        var token = service.GenerateAccessToken(Guid.NewGuid(), "unused", ["Candidate"], out _);

        service.ValidateAccessToken(token).IsValid.Should().BeFalse();
    }

    [Fact]
    public void InvalidSignature_IsRejected()
    {
        var issuer = CreateService(secret: "UNIT_TEST_SECRET_KEY_MUST_BE_LONG_ENOUGH_32");
        var validator = CreateService(secret: "DIFFERENT_SECRET_KEY_MUST_BE_LONG_ENOUGH_32");
        var token = issuer.GenerateAccessToken(Guid.NewGuid(), "unused", ["Admin"], out _);

        validator.ValidateAccessToken(token).IsValid.Should().BeFalse();
    }

    [Fact]
    public void InvalidIssuer_IsRejected()
    {
        var issuer = CreateService(issuer: "OtherIssuer");
        var validator = CreateService(issuer: "IranJob");
        var token = issuer.GenerateAccessToken(Guid.NewGuid(), "unused", ["Admin"], out _);

        validator.ValidateAccessToken(token).IsValid.Should().BeFalse();
    }

    [Fact]
    public void InvalidAudience_IsRejected()
    {
        var issuer = CreateService(audience: "OtherAudience");
        var validator = CreateService(audience: "IranJob.Client");
        var token = issuer.GenerateAccessToken(Guid.NewGuid(), "unused", ["Admin"], out _);

        validator.ValidateAccessToken(token).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GeneratedToken_DoesNotIncludeEmailOrName()
    {
        var service = CreateService();
        var token = service.GenerateAccessToken(Guid.NewGuid(), "user@example.com", ["Candidate"], out _);
        var jwt = new JwtSecurityTokenHandler { MapInboundClaims = false }.ReadJwtToken(token);

        jwt.Claims.Should().NotContain(claim => claim.Type == JwtRegisteredClaimNames.Email);
        jwt.Claims.Should().NotContain(claim => claim.Type == JwtRegisteredClaimNames.UniqueName);
        jwt.Claims.Should().NotContain(claim => claim.Value == "user@example.com");
        jwt.Claims.Should().Contain(claim => claim.Type == JwtRegisteredClaimNames.Sub);
        jwt.Claims.Should().Contain(claim => claim.Type == ClaimTypes.Role && claim.Value == "Candidate");
    }
}
