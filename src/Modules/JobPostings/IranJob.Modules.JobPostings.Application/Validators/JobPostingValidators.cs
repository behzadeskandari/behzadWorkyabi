using FluentValidation;
using IranJob.Modules.JobPostings.Application.Contracts;
using IranJob.Modules.JobPostings.Domain.Enums;

namespace IranJob.Modules.JobPostings.Application.Validators;

public sealed class CreateJobPostingRequestValidator : AbstractValidator<CreateJobPostingRequest>
{
    public CreateJobPostingRequestValidator() => JobPostingInputRules.Apply(this);
}

public sealed class UpdateJobPostingRequestValidator : AbstractValidator<UpdateJobPostingRequest>
{
    public UpdateJobPostingRequestValidator() => JobPostingInputRules.Apply(this);
}

internal static class JobPostingInputRules
{
    public static void Apply<T>(AbstractValidator<T> validator)
        where T : JobPostingInput
    {
        validator.RuleFor(input => input.Title).NotEmpty().MaximumLength(200);
        validator.RuleFor(input => input.Description).NotEmpty().MaximumLength(10000);
        validator.RuleFor(input => input.CategoryId).NotEmpty();
        validator.RuleFor(input => input.SkillIds)
            .NotNull()
            .NotEmpty()
            .WithMessage("At least one skill must be selected.")
            .Must(ids => ids.Count <= 30)
            .WithMessage("No more than 30 skills may be selected.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Skill ids must be unique.")
            .Must(ids => ids.All(id => id != Guid.Empty))
            .WithMessage("Skill ids must be valid.");
        validator.RuleFor(input => input.EmploymentType)
            .Must(value => IsValidEnum<EmploymentType>(value))
            .WithMessage("A valid employment type is required.");
        validator.RuleFor(input => input.WorkArrangement)
            .Must(value => IsValidEnum<WorkArrangement>(value))
            .WithMessage("A valid work arrangement is required.");
        validator.RuleFor(input => input.CityId)
            .NotNull()
            .WithMessage("A city is required for every job posting.");
        validator.RuleFor(input => input.SalaryMinimum)
            .GreaterThanOrEqualTo(0)
            .When(input => input.SalaryMinimum.HasValue);
        validator.RuleFor(input => input.SalaryMaximum)
            .GreaterThanOrEqualTo(0)
            .When(input => input.SalaryMaximum.HasValue);
        validator.RuleFor(input => input.SalaryMaximum)
            .GreaterThanOrEqualTo(input => input.SalaryMinimum!.Value)
            .When(input => input.SalaryMinimum.HasValue && input.SalaryMaximum.HasValue)
            .WithMessage("Salary maximum must be greater than or equal to salary minimum.");
        validator.RuleFor(input => input.SalaryCurrency)
            .Must(IsCurrencyCode)
            .When(HasSalary)
            .WithMessage("A three-letter currency code is required when salary is provided.");
        validator.RuleFor(input => input.SalaryPeriod)
            .Must(value => value is not null && IsValidEnum<SalaryPeriod>(value))
            .When(HasSalary)
            .WithMessage("A valid salary period is required when salary is provided.");
        validator.RuleFor(input => input.SalaryMinimum)
            .NotNull()
            .When(HasSalary)
            .WithMessage("Salary minimum and maximum are required when salary information is provided.");
        validator.RuleFor(input => input.SalaryMaximum)
            .NotNull()
            .When(HasSalary)
            .WithMessage("Salary minimum and maximum are required when salary information is provided.");
        validator.RuleFor(input => input.SalaryMinimum)
            .NotNull()
            .When(input => input.SalaryMaximum.HasValue)
            .WithMessage("Salary minimum and maximum must be provided together.");
        validator.RuleFor(input => input.SalaryMaximum)
            .NotNull()
            .When(input => input.SalaryMinimum.HasValue)
            .WithMessage("Salary minimum and maximum must be provided together.");
        validator.RuleFor(input => input.ClosingAt)
            .Must(closingAt => !closingAt.HasValue || closingAt.Value > DateTimeOffset.UtcNow)
            .WithMessage("Closing date must be in the future.");
    }

    private static bool HasSalary(JobPostingInput input) =>
        input.SalaryMinimum.HasValue || input.SalaryMaximum.HasValue
        || !string.IsNullOrWhiteSpace(input.SalaryCurrency)
        || !string.IsNullOrWhiteSpace(input.SalaryPeriod);

    private static bool IsCurrencyCode(string? value) =>
        value is { Length: 3 } && value.All(char.IsAsciiLetter);

    private static bool IsValidEnum<TEnum>(string? value)
        where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(value, ignoreCase: false, out var parsed) && Enum.IsDefined(parsed);
}
