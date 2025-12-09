using System.Text.RegularExpressions;
using FluentValidation;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for CreateGroupTypeRequest.
/// </summary>
public partial class CreateGroupTypeRequestValidator : AbstractValidator<CreateGroupTypeRequest>
{
    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColorRegex();

    public CreateGroupTypeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Group type name is required")
            .MaximumLength(100).WithMessage("Group type name cannot exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.IconCssClass)
            .MaximumLength(100).WithMessage("Icon CSS class cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.IconCssClass));

        RuleFor(x => x.Color)
            .Must(BeValidHexColor).WithMessage("Color must be a valid hex color code (e.g., #3B82F6)")
            .When(x => !string.IsNullOrEmpty(x.Color));

        RuleFor(x => x.GroupTerm)
            .NotEmpty().WithMessage("Group term is required")
            .MaximumLength(50).WithMessage("Group term cannot exceed 50 characters");

        RuleFor(x => x.GroupMemberTerm)
            .NotEmpty().WithMessage("Group member term is required")
            .MaximumLength(50).WithMessage("Group member term cannot exceed 50 characters");

        RuleFor(x => x.DefaultGroupCapacity)
            .GreaterThan(0).WithMessage("Default group capacity must be greater than 0")
            .When(x => x.DefaultGroupCapacity.HasValue);

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0).WithMessage("Order must be 0 or greater");
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
