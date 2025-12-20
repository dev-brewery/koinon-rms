using FluentValidation;
using Koinon.Application.DTOs.Requests;
using Koinon.Domain.Data;

namespace Koinon.Application.Validators.Giving;

/// <summary>
/// Validator for CreateBatchRequest.
/// </summary>
public class CreateBatchRequestValidator : AbstractValidator<CreateBatchRequest>
{
    public CreateBatchRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Batch name is required")
            .MaximumLength(100).WithMessage("Batch name cannot exceed 100 characters");

        RuleFor(x => x.BatchDate)
            .NotEmpty().WithMessage("Batch date is required");

        RuleFor(x => x.ControlAmount)
            .GreaterThan(0).WithMessage("Control amount must be greater than 0")
            .When(x => x.ControlAmount.HasValue);

        RuleFor(x => x.ControlItemCount)
            .GreaterThan(0).WithMessage("Control item count must be greater than 0")
            .When(x => x.ControlItemCount.HasValue);

        RuleFor(x => x.CampusIdKey)
            .Must(BeValidIdKey).WithMessage("Campus ID must be a valid IdKey format")
            .When(x => !string.IsNullOrWhiteSpace(x.CampusIdKey));

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Note cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Note));
    }

    private static bool BeValidIdKey(string? idKey)
    {
        if (string.IsNullOrWhiteSpace(idKey))
        {
            return false;
        }

        return IdKeyHelper.TryDecode(idKey, out _);
    }
}
