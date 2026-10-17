using FluentValidation;
using IranJob.Modules.Payments.Application.Contracts;

namespace IranJob.Modules.Payments.Application.Validators;

public sealed class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequest>
{
    public static readonly HashSet<string> AcceptedCurrencies = new() { "IRR", "IRT" };

    public CreatePaymentRequestValidator()
    {
        RuleFor(request => request.Amount).GreaterThan(0);
        RuleFor(request => request.Currency)
            .Must(value => value is not null && AcceptedCurrencies.Contains(value))
            .WithMessage("Currency must be one of: IRR, IRT.");
        RuleFor(request => request.Description).MaximumLength(500).When(request => request.Description is not null);
        RuleFor(request => request.CallbackUrl).MaximumLength(1000).When(request => request.CallbackUrl is not null);
    }
}
