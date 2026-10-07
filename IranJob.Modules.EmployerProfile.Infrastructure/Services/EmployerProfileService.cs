using FluentValidation;
using IranJob.Modules.EmployerProfile.Application.Abstractions;
using IranJob.Modules.EmployerProfile.Domain.Entities;
using IranJob.Modules.EmployerProfile.Infrastructure.Persistence;
using IranJob.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using static System.Runtime.InteropServices.JavaScript.JSType;
using DomainValidationException = IranJob.SharedKernel.Exceptions.ValidationException;

namespace IranJob.Modules.EmployerProfile.Infrastructure.Services;

public sealed class EmployerProfileService(
    EmployerDbContext dbContext,
    IValidator<EmployerProfileRequest> requestValidator,
    IWebHostEnvironment environment,
    IHttpContextAccessor httpContextAccessor) : IEmployerProfileService
{
    private const long MaxLogoSizeBytes = 2 * 1024 * 1024; // 2 MB
    private static readonly string[] AllowedLogoMimeTypes =
        { "image/jpeg", "image/png", "image/gif", "image/webp" };

    public async Task<EmployerProfileResult> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.EmployerProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Employer profile was not found.");

        return MapToResult(profile);
    }

    public async Task<EmployerProfileResult> CreateProfileAsync(
        Guid userId,
        EmployerProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var exists = await dbContext.EmployerProfiles
            .AnyAsync(p => p.UserId == userId, cancellationToken);

        if (exists)
            throw new DomainException("An employer profile already exists for this user.");

        var profile = Domain.Entities.EmployerProfile.Create(
            userId,
            request.CompanyName,
            request.Industry,
            request.CompanySize,
            request.City,
            request.Province,
            request.CompanyDescription,
            request.WebsiteUrl,
            request.LinkedInUrl,
            request.LogoUrl,
            request.FoundedYear,
            request.Address,
            request.PostalCode,
            request.ContactEmail,
            request.ContactPhone);

        dbContext.EmployerProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapToResult(profile);
    }

    public async Task<EmployerProfileResult> UpdateProfileAsync(
        Guid userId,
        EmployerProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var profile = await dbContext.EmployerProfiles
            .SingleOrDefaultAsync(p => p.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Employer profile was not found.");

        profile.Update(
            request.CompanyName,
            request.Industry,
            request.CompanySize,
            request.City,
            request.Province,
            request.CompanyDescription,
            request.WebsiteUrl,
            request.LinkedInUrl,
            request.LogoUrl,
            request.FoundedYear,
            request.Address,
            request.PostalCode,
            request.ContactEmail,
            request.ContactPhone);

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapToResult(profile);
    }

    public async Task<EmployerProfileResult> UploadLogoAsync(
        Guid userId,
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSize,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate
        if (fileStream is null || fileSize == 0)
            throw new DomainValidationException(
                new Dictionary<string, string[]> { ["file"] = ["لوگوی شرکت الزامی است."] });

        if (fileSize > MaxLogoSizeBytes)
            throw new DomainValidationException(
                new Dictionary<string, string[]> { ["file"] = ["لوگو نباید بیش از ۲ مگابایت باشد."] });

        if (!AllowedLogoMimeTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            throw new DomainValidationException(
                new Dictionary<string, string[]> { ["file"] = ["فقط فایل‌های تصویری (JPEG, PNG, GIF, WebP) مجاز هستند."] });

        // 2. Save file to disk
        var uploadsFolder = Path.Combine(environment.ContentRootPath, "uploads", "logos");
        Directory.CreateDirectory(uploadsFolder);

        var extension = Path.GetExtension(fileName) ?? ".jpg";
        var uniqueName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(uploadsFolder, uniqueName);

        await using (var stream = new FileStream(physicalPath, FileMode.Create))
        {
            await fileStream.CopyToAsync(stream, cancellationToken);
        }

        var relativePath = $"/uploads/logos/{uniqueName}";

        // 3. Load profile and update LogoUrl
        var profile = await dbContext.EmployerProfiles
            .SingleOrDefaultAsync(p => p.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Employer profile was not found.");

        profile.Update(
            companyName: profile.CompanyName,
            industry: profile.Industry,
            companySize: profile.CompanySize,
            city: profile.City,
            province: profile.Province,
            companyDescription: profile.CompanyDescription,
            websiteUrl: profile.WebsiteUrl,
            linkedInUrl: profile.LinkedInUrl,
            logoUrl: relativePath,               // ← this is the key line
            foundedYear: profile.FoundedYear,
            address: profile.Address,
            postalCode: profile.PostalCode,
            contactEmail: profile.ContactEmail,
            contactPhone: profile.ContactPhone);

        await dbContext.SaveChangesAsync(cancellationToken);

        return MapToResult(profile);
    }

    private async Task ValidateAsync(EmployerProfileRequest request, CancellationToken cancellationToken)
    {
        var validation = await requestValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new DomainValidationException(
                validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
    }

    private EmployerProfileResult MapToResult(Domain.Entities.EmployerProfile entity)
    {
        string? logoUrl = null;

        if (!string.IsNullOrWhiteSpace(entity.LogoUrl))
        {
            var request = httpContextAccessor.HttpContext?.Request;
            if (request is not null)
            {
                logoUrl = $"{request.Scheme}://{request.Host}{entity.LogoUrl}";
            }
            else
            {
                logoUrl = entity.LogoUrl; // fallback
            }
        }

        return new(
            entity.Id,
            entity.UserId,
            entity.CompanyName,
            entity.CompanyDescription,
            entity.Industry,
            entity.CompanySize,
            entity.WebsiteUrl,
            entity.LinkedInUrl,
            logoUrl,
            entity.FoundedYear,
            entity.City,
            entity.Province,
            entity.Address,
            entity.PostalCode,
            entity.ContactEmail,
            entity.ContactPhone,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}