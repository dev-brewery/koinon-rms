using FluentValidation;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for ChangePasswordRequest.
/// Ensures password requirements are met and new password is confirmed.
/// </summary>
public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .WithMessage("Current password is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required")
            .MinimumLength(8)
            .WithMessage("New password must be at least 8 characters")
            .MaximumLength(128)
            .WithMessage("New password cannot exceed 128 characters");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .WithMessage("Password confirmation is required")
            .Equal(x => x.NewPassword)
            .WithMessage("Password confirmation must match new password");

        RuleFor(x => x.NewPassword)
            .Must((request, newPassword) => newPassword != request.CurrentPassword)
            .WithMessage("New password must be different from current password")
            .When(x => !string.IsNullOrEmpty(x.NewPassword) && !string.IsNullOrEmpty(x.CurrentPassword));
    }
}
