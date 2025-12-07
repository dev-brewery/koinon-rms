using FluentValidation;
using Koinon.Application.DTOs.Requests;
using Koinon.Domain.Data;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for AddFamilyMemberRequest.
/// </summary>
public class AddFamilyMemberRequestValidator : AbstractValidator<AddFamilyMemberRequest>
{
    public AddFamilyMemberRequestValidator()
    {
        RuleFor(x => x.PersonId)
            .NotEmpty().WithMessage("Person ID is required")
            .Must(BeValidIdKey).WithMessage("Person ID must be a valid IdKey format");

        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role ID is required")
            .Must(BeValidIdKey).WithMessage("Role ID must be a valid IdKey format");
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
