using FluentValidation;
using Koinon.Application.DTOs;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for CreateCommunicationTemplateDto.
/// </summary>
public class CreateCommunicationTemplateDtoValidator : AbstractValidator<CreateCommunicationTemplateDto>
{
    public CreateCommunicationTemplateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");

        RuleFor(x => x.CommunicationType)
            .NotEmpty().WithMessage("Communication type is required")
            .Must(x => x == "Email" || x == "Sms")
            .WithMessage("Communication type must be Email or Sms");

        RuleFor(x => x.Subject)
            .MaximumLength(500).WithMessage("Subject cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Subject));

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Body is required");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
