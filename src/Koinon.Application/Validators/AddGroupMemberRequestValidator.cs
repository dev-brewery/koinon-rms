using FluentValidation;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for AddGroupMemberRequest.
/// </summary>
public class AddGroupMemberRequestValidator : AbstractValidator<AddGroupMemberRequest>
{
    public AddGroupMemberRequestValidator()
    {
        RuleFor(x => x.PersonId)
            .NotEmpty().WithMessage("Person ID is required");

        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role ID is required");

        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage("Note cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Note));
    }
}
