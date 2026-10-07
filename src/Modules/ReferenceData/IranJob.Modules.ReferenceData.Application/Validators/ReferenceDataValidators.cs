using FluentValidation;
using IranJob.Modules.ReferenceData.Application.Contracts;

namespace IranJob.Modules.ReferenceData.Application.Validators;

public sealed class NamedReferenceDataInputValidator : AbstractValidator<NamedReferenceDataInput>
{
    public NamedReferenceDataInputValidator()
    {
        RuleFor(input => input.Name).NotEmpty().MaximumLength(150);
        RuleFor(input => input.Description).MaximumLength(1000);
    }
}

public sealed class CountryInputValidator : AbstractValidator<CountryInput>
{
    public CountryInputValidator()
    {
        RuleFor(input => input.Name).NotEmpty().MaximumLength(150);
        RuleFor(input => input.Code).NotEmpty().Length(2, 3).Matches("^[A-Za-z]+$");
    }
}

public sealed class ProvinceInputValidator : AbstractValidator<ProvinceInput>
{
    public ProvinceInputValidator()
    {
        RuleFor(input => input.CountryId).NotEmpty();
        RuleFor(input => input.Name).NotEmpty().MaximumLength(150);
    }
}

public sealed class CityInputValidator : AbstractValidator<CityInput>
{
    public CityInputValidator()
    {
        RuleFor(input => input.ProvinceId).NotEmpty();
        RuleFor(input => input.Name).NotEmpty().MaximumLength(150);
    }
}