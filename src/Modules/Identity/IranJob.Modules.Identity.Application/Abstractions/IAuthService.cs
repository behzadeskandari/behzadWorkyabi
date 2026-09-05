namespace IranJob.Modules.Identity.Application.Abstractions;

public interface IAuthService
{
    Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<RefreshResult> RefreshAsync(string? refreshToken, CancellationToken cancellationToken = default);

    Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default);

    Task<UserProfileResult> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string Password,
    string Role);

public sealed record LoginRequest(string Identifier, string Password);

public sealed record LoginResult(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    UserProfileResult User,
    string RefreshToken);

public sealed record RefreshResult(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    UserProfileResult User,
    string RefreshToken);

public sealed record UserProfileResult(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    IReadOnlyList<string> Roles);
