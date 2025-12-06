using FluentValidation;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for AddFamilyMemberRequest.
/// </summary>
public class AddFamilyMemberRequestValidator : AbstractValidator<AddFamilyMemberRequest>
{
    public AddFamilyMemberRequestValidator()
    {
        RuleFor(x => x.PersonId)
            .NotEmpty().WithMessage("Person ID is required");

        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role ID is required");
    }
}
