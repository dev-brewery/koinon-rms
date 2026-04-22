using FluentValidation;
using Koinon.Application.DTOs.Security;

namespace Koinon.Application.Validators.Security;

/// <summary>
/// Validator for <see cref="CreateSecurityRoleRequest"/>.
/// </summary>
public class CreateSecurityRoleRequestValidator : AbstractValidator<CreateSecurityRoleRequest>
{
    public CreateSecurityRoleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Role name is required")
            .MaximumLength(100).WithMessage("Role name cannot exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
