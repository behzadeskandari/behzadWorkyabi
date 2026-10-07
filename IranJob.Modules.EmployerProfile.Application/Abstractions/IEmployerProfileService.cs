using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IranJob.Modules.EmployerProfile.Application.Abstractions;

public interface IEmployerProfileService
{
    Task<EmployerProfileResult> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<EmployerProfileResult> CreateProfileAsync(
        Guid userId,
        EmployerProfileRequest request,
        CancellationToken cancellationToken = default);

    Task<EmployerProfileResult> UpdateProfileAsync(
        Guid userId,
        EmployerProfileRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads the logo, saves the path into the profile, and returns the updated profile.
    /// </summary>
    Task<EmployerProfileResult> UploadLogoAsync(
        Guid userId,
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSize,
        CancellationToken cancellationToken = default);
}

public sealed record EmployerProfileRequest(
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

public sealed record EmployerProfileResult(
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
