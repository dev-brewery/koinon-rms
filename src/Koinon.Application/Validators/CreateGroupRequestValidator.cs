using FluentValidation;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for CreateGroupRequest.
/// </summary>
public class CreateGroupRequestValidator : AbstractValidator<CreateGroupRequest>
{
    public CreateGroupRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Group name is required")
            .MaximumLength(100).WithMessage("Group name cannot exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.GroupTypeId)
            .NotEmpty().WithMessage("Group type is required");

        RuleFor(x => x.GroupCapacity)
            .GreaterThan(0).WithMessage("Group capacity must be greater than 0")
            .When(x => x.GroupCapacity.HasValue);

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0).WithMessage("Order must be 0 or greater");
    }
}
