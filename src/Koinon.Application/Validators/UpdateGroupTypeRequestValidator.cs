using System.Text.RegularExpressions;
using FluentValidation;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for UpdateGroupTypeRequest.
/// </summary>
public partial class UpdateGroupTypeRequestValidator : AbstractValidator<UpdateGroupTypeRequest>
{
    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColorRegex();

    public UpdateGroupTypeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Group type name cannot be empty")
            .MaximumLength(100).WithMessage("Group type name cannot exceed 100 characters")
            .When(x => x.Name != null);

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(x => x.Description != null);

        RuleFor(x => x.IconCssClass)
            .MaximumLength(100).WithMessage("Icon CSS class cannot exceed 100 characters")
            .When(x => x.IconCssClass != null);

        RuleFor(x => x.Color)
            .Must(BeValidHexColor).WithMessage("Color must be a valid hex color code (e.g., #3B82F6)")
            .When(x => x.Color != null);

        RuleFor(x => x.GroupTerm)
            .NotEmpty().WithMessage("Group term cannot be empty")
            .MaximumLength(50).WithMessage("Group term cannot exceed 50 characters")
            .When(x => x.GroupTerm != null);

        RuleFor(x => x.GroupMemberTerm)
            .NotEmpty().WithMessage("Group member term cannot be empty")
            .MaximumLength(50).WithMessage("Group member term cannot exceed 50 characters")
            .When(x => x.GroupMemberTerm != null);

        RuleFor(x => x.DefaultGroupCapacity)
            .GreaterThan(0).WithMessage("Default group capacity must be greater than 0")
            .When(x => x.DefaultGroupCapacity.HasValue);

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0).WithMessage("Order must be 0 or greater")
            .When(x => x.Order.HasValue);
    }

    private static bool BeValidHexColor(string? color)
    {
        if (string.IsNullOrEmpty(color))
        {
            return true;
        }

        return HexColorRegex().IsMatch(color);
    }
}
