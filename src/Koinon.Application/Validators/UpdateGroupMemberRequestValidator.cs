using FluentValidation;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for UpdateGroupMemberRequest.
/// </summary>
public class UpdateGroupMemberRequestValidator : AbstractValidator<UpdateGroupMemberRequest>
{
    public UpdateGroupMemberRequestValidator()
    {
        RuleFor(x => x.Status)
            .Must(status => status == null ||
                           status == "Active" ||
                           status == "Inactive" ||
                           status == "Pending")
            .WithMessage("Status must be one of: Active, Inactive, Pending")
            .When(x => x.Status != null);

        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage("Note cannot exceed 1000 characters")
            .When(x => x.Note != null);
    }
}
