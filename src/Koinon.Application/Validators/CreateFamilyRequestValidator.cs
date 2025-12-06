using FluentValidation;
using Koinon.Application.DTOs.Requests;
using Koinon.Domain.Data;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for CreateFamilyRequest.
/// </summary>
public class CreateFamilyRequestValidator : AbstractValidator<CreateFamilyRequest>
{
    public CreateFamilyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Family name is required")
            .MaximumLength(100).WithMessage("Family name cannot exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.CampusId)
            .Must(BeValidIdKey).WithMessage("Campus ID must be a valid IdKey format")
            .When(x => !string.IsNullOrWhiteSpace(x.CampusId));

        // Address validation
        When(x => x.Address != null, () =>
        {
            RuleFor(x => x.Address!.Street1)
                .NotEmpty().WithMessage("Street address is required")
                .MaximumLength(100).WithMessage("Street address cannot exceed 100 characters");

            RuleFor(x => x.Address!.City)
                .NotEmpty().WithMessage("City is required")
                .MaximumLength(50).WithMessage("City cannot exceed 50 characters");

            RuleFor(x => x.Address!.State)
                .NotEmpty().WithMessage("State is required")
                .MaximumLength(50).WithMessage("State cannot exceed 50 characters");

            RuleFor(x => x.Address!.PostalCode)
                .NotEmpty().WithMessage("Postal code is required")
                .MaximumLength(20).WithMessage("Postal code cannot exceed 20 characters");
        });
    }

    private static bool BeValidIdKey(string? idKey)
    {
        if (string.IsNullOrWhiteSpace(idKey))
        {
            return false;
        }

        return IdKeyHelper.TryDecode(idKey, out _);
    }
}
