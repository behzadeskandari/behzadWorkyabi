using FluentValidation;
using IranJob.Modules.Candidates.Application.Contracts;
using IranJob.Modules.Candidates.Domain.Enums;


/// <summary>
/// Validates the <see cref="SaveResumeRequest"/> payload including all nested section items.
/// </summary>
public sealed class SaveResumeRequestValidator : AbstractValidator<SaveResumeRequest>
{
    public SaveResumeRequestValidator()
    {
        RuleFor(x => x.ProfessionalSummary)
            .MaximumLength(2000)
            .When(x => x.ProfessionalSummary is not null)
            .WithMessage("اطلاعات حرفه\u200cی باید حداکثر ۲۰۰۰ کاراکتر باشد.");

        RuleFor(x => x.Status)
            .Must(BeAValidResumeStatus)
            .When(x => x.Status is not null)
            .WithMessage("وضعیت رزومه نامعتبر است.");

        RuleFor(x => x.WorkExperiences).Must(BeSmallOrEmpty).When(x => x.WorkExperiences is not null)
            .WithMessage("حداکثر ۵۰ سابقه کاری می\u200cتوانید اضافه کنید.");
        RuleFor(x => x.Educations).Must(BeSmallOrEmpty).When(x => x.Educations is not null)
            .WithMessage("حداکثر ۵۰ سواد می\u200cتوانید اضافه کنید.");
        RuleFor(x => x.Skills).Must(BeSmallOrEmpty).When(x => x.Skills is not null)
            .WithMessage("حداکثر ۵۰ مهارت می\u200cتوانید انتخاب کنید.");
        RuleFor(x => x.Languages).Must(BeSmallOrEmpty).When(x => x.Languages is not null)
            .WithMessage("حداکثر ۱۰ زبان می\u200cتوانید اضافه کنید.");
        RuleFor(x => x.Projects).Must(BeSmallOrEmpty).When(x => x.Projects is not null)
            .WithMessage("حداکثر ۵۰ پروژه می\u200cتوانید اضافه کنید.");
        RuleFor(x => x.Certifications).Must(BeSmallOrEmpty).When(x => x.Certifications is not null)
            .WithMessage("حداکثر ۵۰ گواهینامه می\u200cتوانید اضافه کنید.");
        RuleFor(x => x.PortfolioLinks).Must(BeSmallOrEmpty).When(x => x.PortfolioLinks is not null)
            .WithMessage("حداکثر ۲۰ لینک پرتفوی می\u200cتوانید اضافه کنید.");

        RuleForEach(x => x.WorkExperiences).SetValidator(new ResumeWorkExperienceItemValidator());
        RuleForEach(x => x.Educations).SetValidator(new ResumeEducationItemValidator());
        RuleForEach(x => x.Skills).SetValidator(new ResumeSkillItemValidator());
        RuleForEach(x => x.Languages).SetValidator(new ResumeLanguageItemValidator());
        RuleForEach(x => x.Projects).SetValidator(new ResumeProjectItemValidator());
        RuleForEach(x => x.Certifications).SetValidator(new ResumeCertificationItemValidator());
        RuleForEach(x => x.PortfolioLinks).SetValidator(new ResumePortfolioLinkItemValidator());
    }

    private static bool BeAValidResumeStatus(string? value) =>
        string.IsNullOrWhiteSpace(value) || Enum.TryParse<ResumeStatus>(value, ignoreCase: true, out _);
    private static bool BeSmallOrEmpty<T>(IEnumerable<T>? items) => items is null || items.ToList().Count <= 50;
}

public sealed class ResumeWorkExperienceItemValidator : AbstractValidator<ResumeWorkExperienceItem>
{
    public ResumeWorkExperienceItemValidator()
    {
        RuleFor(x => x.Company).NotEmpty().MaximumLength(200).WithMessage("نام شرکت الزامی است.");
        RuleFor(x => x.JobTitle).NotEmpty().MaximumLength(200).WithMessage("عنوان شغل الزامی است.");
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x).Must(item => !item.IsCurrent || item.EndDate is null)
            .WithMessage("اگر سابقه جاری است، تاریخ خاتمه نباید وارد شود.");
    }
}

public sealed class ResumeEducationItemValidator : AbstractValidator<ResumeEducationItem>
{
    public ResumeEducationItemValidator()
    {
        RuleFor(x => x.Institution).NotEmpty().MaximumLength(200).WithMessage("نام مؤسسه الزامی است.");
        RuleFor(x => x.Degree).NotEmpty().MaximumLength(200).WithMessage("مدرک تحصیلی الزامی است.");
        RuleFor(x => x.FieldOfStudy).MaximumLength(200);
        RuleFor(x => x.Grade).MaximumLength(100);
        RuleFor(x => x).Must(item => !item.IsCurrent || item.EndDate is null)
            .WithMessage("اگر تحصیل جاری است، تاریخ خاتمه نباید وارد شود.");
    }
}
public sealed class ResumeSkillItemValidator : AbstractValidator<ResumeSkillItem>
{
    public ResumeSkillItemValidator()
    {
        RuleFor(x => x.SkillId).NotEqual(Guid.Empty).WithMessage("مهارت انتخاب نشده است.");
        RuleFor(x => x.SkillName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ProficiencyLevel).MaximumLength(100);
        RuleFor(x => x.YearsOfExperience).InclusiveBetween(0, 100).When(x => x.YearsOfExperience.HasValue);
    }
}

public sealed class ResumeLanguageItemValidator : AbstractValidator<ResumeLanguageItem>
{
    public ResumeLanguageItemValidator()
    {
        RuleFor(x => x.LanguageName).NotEmpty().MaximumLength(100).WithMessage("نام زبان الزامی است.");
        RuleFor(x => x.ProficiencyLevel).NotEmpty().MaximumLength(100).WithMessage("سطح مهارت الزامی است.");
    }
}

public sealed class ResumeProjectItemValidator : AbstractValidator<ResumeProjectItem>
{
    public ResumeProjectItemValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200).WithMessage("عنوان پروژه الزامی است.");
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Url).MaximumLength(500).Must(BeAValidUrl).When(x => !string.IsNullOrWhiteSpace(x.Url))
            .WithMessage("URL باید یک آدرس http(s) معتبر باشد.");
        RuleFor(x => x).Must(item => !item.IsCurrent || item.EndDate is null)
            .WithMessage("اگر پروژه جاری است، تاریخ خاتمه نباید وارد شود.");
    }

    private static bool BeAValidUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

public sealed class ResumeCertificationItemValidator : AbstractValidator<ResumeCertificationItem>
{
    public ResumeCertificationItemValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).WithMessage("نام گواهینامه الزامی است.");
        RuleFor(x => x.IssuingOrganization).MaximumLength(200);
        RuleFor(x => x.CertificateUrl).MaximumLength(500).Must(BeAValidUrl).When(x => !string.IsNullOrWhiteSpace(x.CertificateUrl))
            .WithMessage("URL گواهینامه باید یک آدرس http(s) معتبر باشد.");
    }

    private static bool BeAValidUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

public sealed class ResumePortfolioLinkItemValidator : AbstractValidator<ResumePortfolioLinkItem>
{
    public ResumePortfolioLinkItemValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200).WithMessage("عنوان لینک الزامی است.");
        RuleFor(x => x.Url).NotEmpty().MaximumLength(500).Must(BeAValidUrl)
            .WithMessage("URL باید یک آدرس http(s) معتبر باشد.");
        RuleFor(x => x.Description).MaximumLength(1000);
    }

    private static bool BeAValidUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}