using FluentValidation;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for UpdateGroupRequest.
/// </summary>
public class UpdateGroupRequestValidator : AbstractValidator<UpdateGroupRequest>
{
    public UpdateGroupRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Group name cannot be empty")
            .MaximumLength(100).WithMessage("Group name cannot exceed 100 characters")
            .When(x => x.Name != null);

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(x => x.Description != null);

        RuleFor(x => x.GroupCapacity)
            .GreaterThan(0).WithMessage("Group capacity must be greater than 0")
            .When(x => x.GroupCapacity.HasValue);

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0).WithMessage("Order must be 0 or greater")
            .When(x => x.Order.HasValue);
    }
}
