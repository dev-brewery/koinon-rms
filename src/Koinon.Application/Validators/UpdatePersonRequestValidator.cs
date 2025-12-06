using FluentValidation;
using Koinon.Application.Constants;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for UpdatePersonRequest.
/// </summary>
public class UpdatePersonRequestValidator : AbstractValidator<UpdatePersonRequest>
{
    public UpdatePersonRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name cannot be empty")
            .MaximumLength(50).WithMessage("First name cannot exceed 50 characters")
            .When(x => x.FirstName != null);

        RuleFor(x => x.NickName)
            .MaximumLength(50).WithMessage("Nick name cannot exceed 50 characters")
            .When(x => x.NickName != null);

        RuleFor(x => x.MiddleName)
            .MaximumLength(50).WithMessage("Middle name cannot exceed 50 characters")
            .When(x => x.MiddleName != null);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name cannot be empty")
            .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters")
            .When(x => x.LastName != null);

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(75).WithMessage("Email cannot exceed 75 characters")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Gender)
            .Must(g => g is null || GenderValues.ValidValues.Contains(g))
            .WithMessage("Gender must be Unknown, Male, or Female")
            .When(x => x.Gender != null);

        RuleFor(x => x.EmailPreference)
            .Must(ep => ep is null || EmailPreferenceValues.ValidValues.Contains(ep))
            .WithMessage("Email preference must be EmailAllowed, NoMassEmails, or DoNotEmail")
            .When(x => x.EmailPreference != null);

        RuleFor(x => x.BirthDate)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Birth date cannot be in the future")
            .When(x => x.BirthDate.HasValue);
    }
}
