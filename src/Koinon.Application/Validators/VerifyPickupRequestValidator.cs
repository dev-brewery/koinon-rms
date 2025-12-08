using FluentValidation;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for VerifyPickupRequest.
/// </summary>
public class VerifyPickupRequestValidator : AbstractValidator<VerifyPickupRequest>
{
    public VerifyPickupRequestValidator()
    {
        // AttendanceIdKey required, not empty
        RuleFor(x => x.AttendanceIdKey)
            .NotEmpty().WithMessage("AttendanceIdKey is required");

        // SecurityCode required, not empty, max length 10
        RuleFor(x => x.SecurityCode)
            .NotEmpty().WithMessage("SecurityCode is required")
            .MaximumLength(10).WithMessage("SecurityCode cannot exceed 10 characters");

        // Either PickupPersonIdKey OR PickupPersonName should be provided (one or both)
        // This is a soft validation - we just warn if both are empty
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.PickupPersonIdKey) ||
                      !string.IsNullOrWhiteSpace(x.PickupPersonName))
            .WithMessage("Either PickupPersonIdKey or PickupPersonName should be provided");
    }
}
