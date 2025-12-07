using FluentValidation;
using Koinon.Application.DTOs;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for FamilySearchParameters.
/// </summary>
public class FamilySearchParametersValidator : AbstractValidator<FamilySearchParameters>
{
    public FamilySearchParametersValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100");
    }
}
