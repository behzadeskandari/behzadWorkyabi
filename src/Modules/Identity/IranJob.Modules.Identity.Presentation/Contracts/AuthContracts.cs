namespace IranJob.Modules.Identity.Presentation.Contracts;

public sealed record RegisterRequestDto(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string Password,
    string Role);

public sealed record LoginRequestDto(string Identifier, string Password);

public sealed record AuthResponseDto(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    UserProfileDto User);

public sealed record UserProfileDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    IReadOnlyList<string> Roles);
