using FluentValidation;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for TwoFactorVerifyRequest.
/// Ensures the verification code is properly formatted.
/// </summary>
public class TwoFactorVerifyRequestValidator : AbstractValidator<TwoFactorVerifyRequest>
{
    public TwoFactorVerifyRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Verification code is required")
            .Matches(@"^\d{6}$")
            .WithMessage("Verification code must be 6 digits");
    }
}
