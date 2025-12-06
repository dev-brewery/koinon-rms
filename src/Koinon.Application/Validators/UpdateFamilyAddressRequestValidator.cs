using FluentValidation;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for UpdateFamilyAddressRequest.
/// </summary>
public class UpdateFamilyAddressRequestValidator : AbstractValidator<UpdateFamilyAddressRequest>
{
    public UpdateFamilyAddressRequestValidator()
    {
        RuleFor(x => x.Street1)
            .MaximumLength(100).WithMessage("Street address cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Street1));

        RuleFor(x => x.Street2)
            .MaximumLength(100).WithMessage("Street address line 2 cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Street2));

        RuleFor(x => x.City)
            .MaximumLength(50).WithMessage("City cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.City));

        RuleFor(x => x.State)
            .MaximumLength(50).WithMessage("State cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.State));

        RuleFor(x => x.PostalCode)
            .MaximumLength(20).WithMessage("Postal code cannot exceed 20 characters")
            .When(x => !string.IsNullOrEmpty(x.PostalCode));

        RuleFor(x => x.Country)
            .MaximumLength(50).WithMessage("Country cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.Country));
    }
}
