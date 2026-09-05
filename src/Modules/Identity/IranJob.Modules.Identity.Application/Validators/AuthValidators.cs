using FluentValidation;
using IranJob.Modules.Identity.Application.Abstractions;
using IranJob.Modules.Identity.Application.Constants;
using IranJob.Modules.Identity.Domain.Constants;

namespace IranJob.Modules.Identity.Application.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Matches(IdentityValidationPatterns.IranianMobilePattern)
            .WithMessage("Phone number must be a valid Iranian mobile number (09xxxxxxxxx).");
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[0-9]").WithMessage("Password must contain a digit.")
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain a non-alphanumeric character.");
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(role => IdentityRoles.PublicRegistrationRoles.Contains(role))
            .WithMessage("Invalid registration role.");
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Identifier).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}
