using FluentValidation;
using Koinon.Application.DTOs;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for UpdateCommunicationTemplateDto.
/// </summary>
public class UpdateCommunicationTemplateDtoValidator : AbstractValidator<UpdateCommunicationTemplateDto>
{
    public UpdateCommunicationTemplateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name cannot be empty")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters")
            .When(x => x.Name != null);

        RuleFor(x => x.CommunicationType)
            .Must(x => x == null || x == "Email" || x == "Sms")
            .WithMessage("CommunicationType must be 'Email' or 'Sms'");

        RuleFor(x => x.Subject)
            .MaximumLength(500).WithMessage("Subject cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Subject));

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Body cannot be empty")
            .When(x => x.Body != null);

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
