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
            .NotNull()
            .WithMessage("A job application is required to start a conversation.");
        RuleFor(x => x.JobApplicationId)
            .Must(id => id.HasValue && id.Value != Guid.Empty)
            .WithMessage("Job application id must be a valid guid.");

        RuleFor(x => x.Subject)
            .MaximumLength(280)
            .WithMessage("Subject cannot exceed 280 characters.");
    }
}
