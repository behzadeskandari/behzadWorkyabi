using FluentValidation;
using IranJob.Modules.Messages.Application.Contracts;

namespace IranJob.Modules.Messages.Application.Validators;

public sealed class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEqual(Guid.Empty)
            .WithMessage("Conversation id is required.");

        RuleFor(x => x.Content)
            .NotEmpty()
            .Must(content => !string.IsNullOrWhiteSpace(content))
            .WithMessage("Message content cannot be blank.")
            .MaximumLength(20000)
            .WithMessage("Message content is required and cannot exceed 20,000 characters.");
    }
}
