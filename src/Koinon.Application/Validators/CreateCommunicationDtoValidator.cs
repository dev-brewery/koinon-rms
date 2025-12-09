using FluentValidation;
using Koinon.Application.DTOs;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for CreateCommunicationDto.
/// </summary>
public class CreateCommunicationDtoValidator : AbstractValidator<CreateCommunicationDto>
{
    public CreateCommunicationDtoValidator()
    {
        RuleFor(x => x.GroupIdKeys)
            .NotEmpty().WithMessage("At least one group must be specified")
            .Must(x => x.Count <= 50).WithMessage("Cannot specify more than 50 groups per communication");

        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Subject is required for email communications")
            .When(x => x.CommunicationType == "Email");

        RuleFor(x => x.Subject)
            .MaximumLength(500).WithMessage("Subject cannot exceed 500 characters")
            .Must(x => !ContainsHeaderInjection(x)).WithMessage("Subject contains invalid characters")
            .When(x => !string.IsNullOrEmpty(x.Subject));

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Body is required")
            .MaximumLength(100000).WithMessage("Body cannot exceed 100,000 characters");

        // SMS-specific body validation (10 segments max @ 160 chars per segment)
        RuleFor(x => x.Body)
            .MaximumLength(1600).WithMessage("SMS body cannot exceed 1600 characters (10 segments)")
            .When(x => x.CommunicationType == "Sms");

        RuleFor(x => x.CommunicationType)
            .NotEmpty().WithMessage("Communication type is required")
            .Must(x => x == "Email" || x == "Sms")
            .WithMessage("Communication type must be Email or Sms");

        // SMS should not have subject
        RuleFor(x => x.Subject)
            .Empty().WithMessage("Subject should not be set for SMS communications")
            .When(x => x.CommunicationType == "Sms");

        RuleFor(x => x.FromEmail)
            .NotEmpty().WithMessage("FromEmail is required for email communications")
            .When(x => x.CommunicationType == "Email");

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
    }

    private static bool ContainsHeaderInjection(string? value) =>
        value != null && (value.Contains('\n') || value.Contains('\r') || value.Contains('\0'));
}
