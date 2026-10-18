using FluentValidation;
using IranJob.Modules.Candidates.Application.Abstractions;
using IranJob.Modules.Candidates.Application.Contracts;
using IranJob.Modules.Candidates.Domain.Entities;
using IranJob.Modules.Candidates.Domain.Enums;
using IranJob.Modules.Candidates.Infrastructure.Persistence;
using IranJob.SharedKernel.Exceptions;
using Microsoft.EntityFrameworkCore;
using DomainValidationException = IranJob.SharedKernel.Exceptions.ValidationException;

namespace IranJob.Modules.Candidates.Infrastructure.Services;

public sealed class CandidateProfileService(
    CandidateDbContext dbContext,
    IValidator<CandidateProfileRequest> requestValidator) : ICandidateProfileService
{
    public async Task<CandidateProfileResult> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.CandidateProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(candidateProfile => candidateProfile.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Candidate profile was not found.");

        return MapToResult(profile);
    }

    public async Task<CandidateProfileResult> CreateProfileAsync(
        Guid userId,
        CandidateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var exists = await dbContext.CandidateProfiles
            .AnyAsync(candidateProfile => candidateProfile.UserId == userId, cancellationToken);

        if (exists)
        {
            throw new DomainException("A candidate profile already exists for this user.");
        }

        var profile = new CandidateProfile
        {
            UserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        ApplyRequest(profile, request);

        dbContext.CandidateProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapToResult(profile);
    }

    public async Task<CandidateProfileResult> UpdateProfileAsync(
        Guid userId,
        CandidateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var profile = await dbContext.CandidateProfiles
            .SingleOrDefaultAsync(candidateProfile => candidateProfile.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Candidate profile was not found.");

        ApplyRequest(profile, request);
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return MapToResult(profile);
    }

    private async Task ValidateAsync(CandidateProfileRequest request, CancellationToken cancellationToken)
    {
        var validation = await requestValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new DomainValidationException(validation.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));
        }
    }

    private static void ApplyRequest(CandidateProfile profile, CandidateProfileRequest request)
    {
        profile.Headline = TrimToNull(request.Headline);
        profile.Biography = TrimToNull(request.Biography);
        profile.DateOfBirth = request.DateOfBirth;
        profile.Gender = ParseEnum<Gender>(request.Gender);
        profile.City = TrimToNull(request.City);
        profile.Province = TrimToNull(request.Province);
        profile.LinkedInUrl = TrimToNull(request.LinkedInUrl);
        profile.GitHubUrl = TrimToNull(request.GitHubUrl);
        profile.PortfolioUrl = TrimToNull(request.PortfolioUrl);
        profile.ExpectedSalary = request.ExpectedSalary;
        profile.SalaryType = ParseEnum<SalaryType>(request.SalaryType);
        profile.EmploymentStatus = ParseEnum<EmploymentStatus>(request.EmploymentStatus);
        profile.Availability = ParseEnum<Availability>(request.Availability);
        profile.MilitaryStatus = ParseEnum<MilitaryStatus>(request.MilitaryStatus);
    }

    private static string? TrimToNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static TEnum? ParseEnum<TEnum>(string? value)
        where TEnum : struct, Enum =>
        string.IsNullOrWhiteSpace(value) ? null : Enum.Parse<TEnum>(value, ignoreCase: false);

    public async Task<ProfileCompletionResponse?> GetCompletionStatusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.CandidateProfiles.AsNoTracking()
            .SingleOrDefaultAsync(candidateProfile => candidateProfile.UserId == userId, cancellationToken);

        if (profile is null)
            return null;

        var missing = GetMissingProfileFields(profile);
        return new ProfileCompletionResponse(
            CompletenessScore: CalculateProfileCompleteness(profile, missing),
            HasProfile: true,
            MissingFields: missing);
    }

    private static CandidateProfileResult MapToResult(CandidateProfile profile) =>
        new(
            profile.Id,
            profile.UserId,
            profile.Headline,
            profile.Biography,
            profile.DateOfBirth,
            profile.Gender?.ToString(),
            profile.City,
            profile.Province,
            profile.LinkedInUrl,
            profile.GitHubUrl,
            profile.PortfolioUrl,
            profile.ExpectedSalary,
            profile.SalaryType?.ToString(),
            profile.EmploymentStatus?.ToString(),
            profile.Availability?.ToString(),
            profile.MilitaryStatus?.ToString(),
            profile.CreatedAt,
            profile.UpdatedAt);

    private static string[] GetMissingProfileFields(CandidateProfile profile)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(profile.Headline)) missing.Add("Headline");
        if (string.IsNullOrWhiteSpace(profile.City)) missing.Add("City");
        if (string.IsNullOrWhiteSpace(profile.Province)) missing.Add("Province");
        if (string.IsNullOrWhiteSpace(profile.Biography)) missing.Add("Biography");
        if (profile.Gender is null) missing.Add("Gender");
        if (profile.EmploymentStatus is null) missing.Add("EmploymentStatus");
        if (profile.Availability is null) missing.Add("Availability");
        return missing.ToArray();
    }

    private static int CalculateProfileCompleteness(CandidateProfile profile, string[] missing)
    {
        // Required: headline + city + province + biography + gender = 40 (10 each)
        // Optional but recommended: employment status + availability + salary = 60
        var max = 100;
        var score = Math.Max(0, max - missing.Length * 10);
        return score;
    }
}
