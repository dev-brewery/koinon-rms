using FluentValidation;
using Koinon.Application.DTOs;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for UpdateCommunicationDto.
/// </summary>
public class UpdateCommunicationDtoValidator : AbstractValidator<UpdateCommunicationDto>
{
    public UpdateCommunicationDtoValidator()
    {
        RuleFor(x => x.Subject)
            .MaximumLength(500).WithMessage("Subject cannot exceed 500 characters")
            .Must(x => !ContainsHeaderInjection(x)).WithMessage("Subject contains invalid characters")
            .When(x => !string.IsNullOrEmpty(x.Subject));

        RuleFor(x => x.FromEmail)
            .EmailAddress().WithMessage("Invalid email address")
            .MaximumLength(254).WithMessage("From email cannot exceed 254 characters")
            .When(x => !string.IsNullOrEmpty(x.FromEmail));

        RuleFor(x => x.FromName)
            .MaximumLength(200).WithMessage("From name cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.FromName));

        RuleFor(x => x.ReplyToEmail)
            .EmailAddress().WithMessage("Invalid email address")
            .MaximumLength(254).WithMessage("Reply-to email cannot exceed 254 characters")
            .When(x => !string.IsNullOrEmpty(x.ReplyToEmail));

        RuleFor(x => x.Body)
            .MaximumLength(100000).WithMessage("Body cannot exceed 100,000 characters")
            .When(x => !string.IsNullOrEmpty(x.Body));
    }

    private static bool ContainsHeaderInjection(string? value) =>
        value != null && (value.Contains('\n') || value.Contains('\r') || value.Contains('\0'));
}
