using FluentValidation;
using IranJob.Modules.Messages.Application.Contracts;

namespace IranJob.Modules.Messages.Application.Validators;

public sealed class CreateConversationRequestValidator : AbstractValidator<CreateConversationRequest>
{
    public CreateConversationRequestValidator()
    {
        RuleFor(x => x.JobPostingId)
            .NotEqual(Guid.Empty)
            .WithMessage("Job posting id is required.");

        RuleFor(x => x.JobApplicationId)
            .GreaterThan(Guid.Empty)
            .When(x => x.JobApplicationId.HasValue)
            .WithMessage("Job application id must be a valid guid when supplied.");

        RuleFor(x => x.Subject)
            .MaximumLength(280)
            .WithMessage("Subject cannot exceed 280 characters.");
    }
}
