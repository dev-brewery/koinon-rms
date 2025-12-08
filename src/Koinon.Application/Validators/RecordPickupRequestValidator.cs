using FluentValidation;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for RecordPickupRequest.
/// </summary>
public class RecordPickupRequestValidator : AbstractValidator<RecordPickupRequest>
{
    public RecordPickupRequestValidator()
    {
        // AttendanceIdKey required, not empty
        RuleFor(x => x.AttendanceIdKey)
            .NotEmpty().WithMessage("AttendanceIdKey is required");

        // Either PickupPersonIdKey OR PickupPersonName must be provided
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.PickupPersonIdKey) ||
                      !string.IsNullOrWhiteSpace(x.PickupPersonName))
            .WithMessage("Either PickupPersonIdKey or PickupPersonName must be provided");

        // If SupervisorOverride is true, SupervisorPersonIdKey must be provided
        RuleFor(x => x.SupervisorPersonIdKey)
            .NotEmpty()
            .WithMessage("SupervisorPersonIdKey is required when SupervisorOverride is true")
            .When(x => x.SupervisorOverride);

        // If WasAuthorized is false, SupervisorOverride should be true (business rule warning)
        RuleFor(x => x.SupervisorOverride)
            .Equal(true)
            .WithMessage("SupervisorOverride should be true when WasAuthorized is false")
            .When(x => !x.WasAuthorized);

        // Notes max length 1000 if provided
        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }
}
