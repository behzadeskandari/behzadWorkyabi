using FluentValidation;
using IranJob.Modules.Candidates.Application.Abstractions;
using IranJob.Modules.Candidates.Application.Constants;
using IranJob.Modules.Candidates.Domain.Enums;

namespace IranJob.Modules.Candidates.Application.Validators;

public sealed class CandidateProfileRequestValidator : AbstractValidator<CandidateProfileRequest>
{
    private const decimal MaxExpectedSalary = 1_000_000_000m;

    public CandidateProfileRequestValidator()
    {
        RuleFor(x => x.Headline)
            .MaximumLength(200);

        RuleFor(x => x.Biography)
            .MaximumLength(4000);

        RuleFor(x => x.DateOfBirth)
            .Must(BeAValidDateOfBirth)
            .When(x => x.DateOfBirth.HasValue)
            .WithMessage("Date of birth must be between 1900-01-01 and today.");

        RuleFor(x => x.Gender)
            .Must(value => IsValidEnum<Gender>(value))
            .When(x => !string.IsNullOrWhiteSpace(x.Gender))
            .WithMessage("Invalid gender value.");

        RuleFor(x => x.City)
            .MaximumLength(100);

        RuleFor(x => x.Province)
            .MaximumLength(100);

        RuleFor(x => x.Phone)
            .Matches(CandidateValidationPatterns.IranianMobilePattern)
            .When(x => !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage("Phone number must be a valid Iranian mobile number (09xxxxxxxxx).");

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Email address is not valid.");

        RuleFor(x => x.Email)
            .MaximumLength(256);

        RuleFor(x => x.LinkedInUrl)
            .Must(BeAValidUrl)
            .When(x => !string.IsNullOrWhiteSpace(x.LinkedInUrl))
            .WithMessage("LinkedIn URL must be a valid absolute http(s) URL.");

        RuleFor(x => x.LinkedInUrl)
            .MaximumLength(500);

        RuleFor(x => x.GitHubUrl)
            .Must(BeAValidUrl)
            .When(x => !string.IsNullOrWhiteSpace(x.GitHubUrl))
            .WithMessage("GitHub URL must be a valid absolute http(s) URL.");

        RuleFor(x => x.GitHubUrl)
            .MaximumLength(500);

        RuleFor(x => x.PortfolioUrl)
            .Must(BeAValidUrl)
            .When(x => !string.IsNullOrWhiteSpace(x.PortfolioUrl))
            .WithMessage("Portfolio URL must be a valid absolute http(s) URL.");

        RuleFor(x => x.PortfolioUrl)
            .MaximumLength(500);

        RuleFor(x => x.ExpectedSalary)
            .InclusiveBetween(0m, MaxExpectedSalary)
            .When(x => x.ExpectedSalary.HasValue)
            .WithMessage($"Expected salary must be between 0 and {MaxExpectedSalary}.");

        RuleFor(x => x.SalaryType)
            .Must(value => IsValidEnum<SalaryType>(value))
            .When(x => !string.IsNullOrWhiteSpace(x.SalaryType))
            .WithMessage("Invalid salary type value.");

        RuleFor(x => x.SalaryType)
            .NotEmpty()
            .When(x => x.ExpectedSalary.HasValue)
            .WithMessage("Salary type is required when an expected salary is provided.");

        RuleFor(x => x.EmploymentStatus)
            .Must(value => IsValidEnum<EmploymentStatus>(value))
            .When(x => !string.IsNullOrWhiteSpace(x.EmploymentStatus))
            .WithMessage("Invalid employment status value.");

        RuleFor(x => x.Availability)
            .Must(value => IsValidEnum<Availability>(value))
            .When(x => !string.IsNullOrWhiteSpace(x.Availability))
            .WithMessage("Invalid availability value.");

        RuleFor(x => x.MilitaryStatus)
            .Must(value => IsValidEnum<MilitaryStatus>(value))
            .When(x => !string.IsNullOrWhiteSpace(x.MilitaryStatus))
            .WithMessage("Invalid military status value.");
    }

    private static bool BeAValidDateOfBirth(DateOnly? dateOfBirth) =>
        dateOfBirth >= new DateOnly(1900, 1, 1) && dateOfBirth <= DateOnly.FromDateTime(DateTime.UtcNow);

    private static bool BeAValidUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static bool IsValidEnum<TEnum>(string? value)
        where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(value, ignoreCase: false, out _);
}
