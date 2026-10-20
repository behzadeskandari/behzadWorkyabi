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
            .MaximumLength(20000)
            .WithMessage("Message content cannot exceed 20,000 characters.");
    }
}
