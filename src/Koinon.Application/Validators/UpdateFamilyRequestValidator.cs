using FluentValidation;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for UpdateFamilyRequest.
/// </summary>
public class UpdateFamilyRequestValidator : AbstractValidator<UpdateFamilyRequest>
{
    public UpdateFamilyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Family name cannot be empty when provided")
            .MaximumLength(100).WithMessage("Family name cannot exceed 100 characters")
            .When(x => x.Name != null);

        RuleFor(x => x.CampusId)
            .Matches(@"^[A-Za-z0-9_-]{10,30}$").WithMessage("Campus ID must be a valid IdKey format")
            .When(x => x.CampusId != null);
    }
}
