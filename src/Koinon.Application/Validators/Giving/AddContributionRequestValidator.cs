using FluentValidation;
using Koinon.Application.DTOs.Requests;
using Koinon.Domain.Data;

namespace Koinon.Application.Validators.Giving;

/// <summary>
/// Validator for AddContributionRequest.
/// </summary>
public class AddContributionRequestValidator : AbstractValidator<AddContributionRequest>
{
    public AddContributionRequestValidator()
    {
        RuleFor(x => x.TransactionDateTime)
            .NotEmpty().WithMessage("Transaction date/time is required");

        RuleFor(x => x.TransactionTypeValueIdKey)
            .NotEmpty().WithMessage("Transaction type is required")
            .Must(BeValidIdKey).WithMessage("Transaction type must be a valid IdKey format");

        RuleFor(x => x.Details)
            .NotEmpty().WithMessage("At least one contribution detail is required");

        RuleForEach(x => x.Details)
            .SetValidator(new ContributionDetailRequestValidator());

        RuleFor(x => x.PersonIdKey)
            .Must(BeValidIdKey).WithMessage("Person ID must be a valid IdKey format")
            .When(x => !string.IsNullOrWhiteSpace(x.PersonIdKey));

        RuleFor(x => x.TransactionCode)
            .MaximumLength(50).WithMessage("Transaction code cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.TransactionCode));

        RuleFor(x => x.Summary)
            .MaximumLength(500).WithMessage("Summary cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Summary));
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

/// <summary>
/// Validator for ContributionDetailRequest.
/// </summary>
public class ContributionDetailRequestValidator : AbstractValidator<ContributionDetailRequest>
{
    public ContributionDetailRequestValidator()
    {
        RuleFor(x => x.FundIdKey)
            .NotEmpty().WithMessage("Fund is required")
            .Must(BeValidIdKey).WithMessage("Fund ID must be a valid IdKey format");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0");

        RuleFor(x => x.Summary)
            .MaximumLength(500).WithMessage("Summary cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Summary));
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
