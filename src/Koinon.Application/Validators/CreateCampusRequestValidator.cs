using FluentValidation;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for CreateCampusRequest.
/// </summary>
public class CreateCampusRequestValidator : AbstractValidator<CreateCampusRequest>
{
    public CreateCampusRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Campus name is required")
            .MaximumLength(100).WithMessage("Campus name cannot exceed 100 characters");

        RuleFor(x => x.ShortCode)
            .MaximumLength(20).WithMessage("Short code cannot exceed 20 characters")
            .When(x => !string.IsNullOrEmpty(x.ShortCode));

        RuleFor(x => x.Url)
            .MaximumLength(255).WithMessage("URL cannot exceed 255 characters")
            .When(x => !string.IsNullOrEmpty(x.Url));

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.TimeZoneId)
            .MaximumLength(50).WithMessage("Time zone ID cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.TimeZoneId));
    }
}
