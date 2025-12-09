using FluentValidation;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for UpdateFamilyMemberRequest.
/// Ensures field constraints for updating family member information.
/// </summary>
public class UpdateFamilyMemberRequestValidator : AbstractValidator<UpdateFamilyMemberRequest>
{
    public UpdateFamilyMemberRequestValidator()
    {
        When(x => !string.IsNullOrWhiteSpace(x.NickName), () =>
        {
            RuleFor(x => x.NickName)
                .MaximumLength(50)
                .WithMessage("NickName cannot exceed 50 characters");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Allergies), () =>
        {
            RuleFor(x => x.Allergies)
                .MaximumLength(500)
                .WithMessage("Allergies cannot exceed 500 characters")
                .Must(ContainNoHtmlOrScripts!)
                .WithMessage("Allergies field cannot contain HTML or script tags");
        });

        When(x => !string.IsNullOrWhiteSpace(x.SpecialNeeds), () =>
        {
            RuleFor(x => x.SpecialNeeds)
                .MaximumLength(2000)
                .WithMessage("SpecialNeeds cannot exceed 2000 characters")
                .Must(ContainNoHtmlOrScripts!)
                .WithMessage("Special needs field cannot contain HTML or script tags");
        });

        RuleFor(x => x.PhoneNumbers)
            .Must(phones => phones == null || phones.Count <= 10)
            .WithMessage("Maximum of 10 phone numbers allowed");

        When(x => x.PhoneNumbers != null && x.PhoneNumbers.Any(), () =>
        {
            RuleForEach(x => x.PhoneNumbers)
                .SetValidator(new UpdatePhoneNumberRequestValidator());
        });
    }

    private static bool ContainNoHtmlOrScripts(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        // Check for HTML tags
        if (System.Text.RegularExpressions.Regex.IsMatch(value, @"<[^>]+>"))
        {
            return false;
        }

        // Check for script patterns
        if (value.Contains("javascript:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (value.Contains("onclick", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (value.Contains("onerror", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
}
