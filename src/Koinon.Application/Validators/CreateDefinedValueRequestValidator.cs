using FluentValidation;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for CreateDefinedValueRequest.
/// </summary>
public class CreateDefinedValueRequestValidator : AbstractValidator<CreateDefinedValueRequest>
{
    public CreateDefinedValueRequestValidator()
    {
        RuleFor(x => x.Value)
            .NotEmpty().WithMessage("Value is required")
            .MaximumLength(200).WithMessage("Value cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
