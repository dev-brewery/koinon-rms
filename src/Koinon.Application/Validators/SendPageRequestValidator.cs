using FluentValidation;
using Koinon.Application.DTOs.Requests;
using Koinon.Domain.Enums;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for SendPageRequest to ensure required fields are provided.
/// </summary>
public class SendPageRequestValidator : AbstractValidator<SendPageRequest>
{
    public SendPageRequestValidator()
    {
        RuleFor(x => x.PagerNumber)
            .NotEmpty().WithMessage("Pager number is required");

        RuleFor(x => x.CustomMessage)
            .NotEmpty()
            .When(x => x.MessageType == PagerMessageType.Custom)
            .WithMessage("Custom message text is required when MessageType is Custom");
    }
}
