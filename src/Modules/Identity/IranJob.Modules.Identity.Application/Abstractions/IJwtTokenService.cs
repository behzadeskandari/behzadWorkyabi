namespace IranJob.Modules.Identity.Application.Abstractions;

public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string userName, IReadOnlyList<string> roles, out DateTimeOffset expiresAt);

    (bool IsValid, Guid UserId, IReadOnlyList<string> Roles) ValidateAccessToken(string token);
}
