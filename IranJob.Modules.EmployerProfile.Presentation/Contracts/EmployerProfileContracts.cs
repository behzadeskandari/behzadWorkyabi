using Microsoft.AspNetCore.Http;

namespace IranJob.Modules.EmployerProfile.Presentation.Contracts;

public sealed record SaveEmployerProfileRequestDto(
    string CompanyName,
    string Industry,
    string CompanySize,
    string City,
    string Province,
    string? CompanyDescription,
    string? WebsiteUrl,
    string? LinkedInUrl,
    string? LogoUrl,
    int? FoundedYear,
    string? Address,
    string? PostalCode,
    string? ContactEmail,
    string? ContactPhone);

public sealed record EmployerProfileResponseDto(
    Guid Id,
    Guid UserId,
    string CompanyName,
    string? CompanyDescription,
    string Industry,
    string CompanySize,
    string? WebsiteUrl,
    string? LinkedInUrl,
    string? LogoUrl,
    int? FoundedYear,
    string City,
    string Province,
    string? Address,
    string? PostalCode,
    string? ContactEmail,
    string? ContactPhone,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record LogoUploadRequestDto(IFormFile File);

public sealed record LogoUploadResponseDto(string LogoUrl);
