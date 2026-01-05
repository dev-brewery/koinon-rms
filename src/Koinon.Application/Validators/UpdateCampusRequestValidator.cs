using FluentValidation;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for UpdateCampusRequest.
/// </summary>
public class UpdateCampusRequestValidator : AbstractValidator<UpdateCampusRequest>
{
    public UpdateCampusRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Campus name cannot be empty")
            .MaximumLength(100).WithMessage("Campus name cannot exceed 100 characters")
            .When(x => x.Name != null);

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
