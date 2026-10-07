using FluentValidation;
using IranJob.Modules.JobPostings.Application.Contracts;
using IranJob.Modules.JobPostings.Domain.Enums;

namespace IranJob.Modules.JobPostings.Application.Validators;

public sealed class PublicJobSearchQueryValidator : AbstractValidator<PublicJobSearchQuery>
{
    private static readonly string[] SortOptions = ["Newest", "Oldest", "SalaryAscending", "SalaryDescending"];

    public PublicJobSearchQueryValidator()
    {
        RuleFor(query => query.Q).MaximumLength(200);
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query)
            .Must(query => query.Page > 0 && ((long)query.Page - 1) * query.PageSize <= int.MaxValue)
            .WithMessage("The requested page offset is too large.");
        RuleFor(query => query.Sort).Must(value => SortOptions.Contains(value, StringComparer.Ordinal))
            .WithMessage("A supported sort order is required.");
        RuleFor(query => query.SkillIds).Must(ids => ids.Count <= 10)
            .WithMessage("No more than 10 skills may be selected.");
        RuleFor(query => query.SkillIds).Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Skill ids must be unique.");
        RuleFor(query => query.SalaryMinimum).GreaterThanOrEqualTo(0).When(query => query.SalaryMinimum.HasValue);
        RuleFor(query => query.SalaryMaximum).GreaterThanOrEqualTo(0).When(query => query.SalaryMaximum.HasValue);
        RuleFor(query => query.SalaryMaximum)
            .GreaterThanOrEqualTo(query => query.SalaryMinimum!.Value)
            .When(query => query.SalaryMinimum.HasValue && query.SalaryMaximum.HasValue)
            .WithMessage("Salary maximum must be greater than or equal to salary minimum.");
        RuleFor(query => query.SalaryCurrency)
            .Must(value => value is { Length: 3 } && value.All(char.IsAsciiLetter))
            .When(query => query.SalaryCurrency is not null || query.SalaryMinimum.HasValue || query.SalaryMaximum.HasValue
                || query.Sort is "SalaryAscending" or "SalaryDescending")
            .WithMessage("A three-letter salary currency is required with salary filtering or sorting.");
        RuleFor(query => query.WorkArrangement)
            .Must(IsDefined<WorkArrangement>)
            .When(query => query.WorkArrangement is not null)
            .WithMessage("A supported work arrangement is required.");
        RuleFor(query => query.EmploymentType)
            .Must(IsDefined<EmploymentType>)
            .When(query => query.EmploymentType is not null)
            .WithMessage("A supported employment type is required.");
    }

    private static bool IsDefined<TEnum>(string? value)
        where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(value, ignoreCase: false, out var parsed) && Enum.IsDefined(parsed);
}
