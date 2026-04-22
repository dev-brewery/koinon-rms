using FluentValidation;
using Koinon.Application.DTOs.Security;

namespace Koinon.Application.Validators.Security;

/// <summary>
/// Validator for <see cref="AssignClaimRequest"/>.
/// </summary>
public class AssignClaimRequestValidator : AbstractValidator<AssignClaimRequest>
{
    public AssignClaimRequestValidator()
    {
        RuleFor(x => x.ClaimIdKey)
            .NotEmpty().WithMessage("ClaimIdKey is required");

        RuleFor(x => x.AllowOrDeny)
            .Must(value => value == 'A' || value == 'D')
            .WithMessage("AllowOrDeny must be either 'A' (Allow) or 'D' (Deny)");
    }
}
