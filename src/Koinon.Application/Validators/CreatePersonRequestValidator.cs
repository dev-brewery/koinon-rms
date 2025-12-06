using FluentValidation;
using Koinon.Application.Constants;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for CreatePersonRequest.
/// </summary>
public class CreatePersonRequestValidator : AbstractValidator<CreatePersonRequest>
{
    public CreatePersonRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(50).WithMessage("First name cannot exceed 50 characters");

        RuleFor(x => x.NickName)
            .MaximumLength(50).WithMessage("Nick name cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.NickName));

        RuleFor(x => x.MiddleName)
            .MaximumLength(50).WithMessage("Middle name cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.MiddleName));

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(75).WithMessage("Email cannot exceed 75 characters")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Gender)
            .Must(g => g is null || GenderValues.ValidValues.Contains(g))
            .WithMessage("Gender must be Unknown, Male, or Female");

        RuleFor(x => x.BirthDate)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Birth date cannot be in the future")
            .When(x => x.BirthDate.HasValue);

        RuleFor(x => x.PhoneNumbers)
            .Must(phones => phones == null || phones.All(p => !string.IsNullOrWhiteSpace(p.Number)))
            .WithMessage("Phone number is required for all phone number entries")
            .When(x => x.PhoneNumbers != null && x.PhoneNumbers.Any());

        RuleFor(x => x.FamilyName)
            .MaximumLength(100).WithMessage("Family name cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.FamilyName));
    }
}
