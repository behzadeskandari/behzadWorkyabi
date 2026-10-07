using FluentValidation;
using IranJob.Modules.EmployerProfile.Application.Abstractions;

namespace IranJob.Modules.EmployerProfile.Application.Validators;

public sealed class EmployerProfileRequestValidator : AbstractValidator<EmployerProfileRequest>
{
    public EmployerProfileRequestValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Company name is required.");

        RuleFor(x => x.Industry)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Industry is required.");

        RuleFor(x => x.CompanySize)
            .NotEmpty()
            .MaximumLength(50)
            .WithMessage("Company size is required.");

        RuleFor(x => x.City)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("City is required.");

        RuleFor(x => x.Province)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Province is required.");

        RuleFor(x => x.CompanyDescription)
            .MaximumLength(2000)
            .WithMessage("Company description must be 2000 characters or less.");

        RuleFor(x => x.WebsiteUrl)
            .Must(BeAValidUrl)
            .When(x => !string.IsNullOrWhiteSpace(x.WebsiteUrl))
            .WithMessage("Website URL must be a valid absolute http(s) URL.");

        RuleFor(x => x.WebsiteUrl)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.WebsiteUrl))
            .WithMessage("Website URL must be 500 characters or less.");

        RuleFor(x => x.LinkedInUrl)
            .Must(BeAValidUrl)
            .When(x => !string.IsNullOrWhiteSpace(x.LinkedInUrl))
            .WithMessage("LinkedIn URL must be a valid absolute http(s) URL.");

        RuleFor(x => x.LinkedInUrl)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.LinkedInUrl))
            .WithMessage("LinkedIn URL must be 500 characters or less.");

        RuleFor(x => x.LogoUrl)
            .Must(BeAValidUrl)
            .When(x => !string.IsNullOrWhiteSpace(x.LogoUrl))
            .WithMessage("Logo URL must be a valid absolute http(s) URL.");

        RuleFor(x => x.LogoUrl)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.LogoUrl))
            .WithMessage("Logo URL must be 500 characters or less.");

        RuleFor(x => x.FoundedYear)
            .InclusiveBetween(1800, DateTime.UtcNow.Year)
            .When(x => x.FoundedYear.HasValue)
            .WithMessage("Founded year must be a valid year.");

        RuleFor(x => x.Address)
            .MaximumLength(500)
            .WithMessage("Address must be 500 characters or less.");

        RuleFor(x => x.PostalCode)
            .MaximumLength(20)
            .WithMessage("Postal code must be 20 characters or less.");

        RuleFor(x => x.ContactEmail)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail))
            .WithMessage("Contact email is not valid.");

        RuleFor(x => x.ContactPhone)
            .MaximumLength(50)
            .WithMessage("Contact phone must be 50 characters or less.");
    }

    private static bool BeAValidUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
