using FluentValidation;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for KioskFamilyRegistrationRequest.
/// Enforces minimal kiosk input requirements for first-time family registration.
/// </summary>
public class KioskFamilyRegistrationRequestValidator : AbstractValidator<KioskFamilyRegistrationRequest>
{
    public KioskFamilyRegistrationRequestValidator()
    {
        RuleFor(x => x.ParentFirstName)
            .NotEmpty().WithMessage("Parent first name is required")
            .MaximumLength(100).WithMessage("Parent first name cannot exceed 100 characters");

        RuleFor(x => x.ParentLastName)
            .NotEmpty().WithMessage("Parent last name is required")
            .MaximumLength(100).WithMessage("Parent last name cannot exceed 100 characters");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required")
            .Must(phone =>
            {
                if (string.IsNullOrWhiteSpace(phone))
                {
                    return false;
                }

                var digits = new string(phone.Where(char.IsDigit).ToArray());
                return digits.Length == 10;
            })
            .WithMessage("Phone number must contain exactly 10 digits");

        RuleFor(x => x.Children)
            .NotEmpty().WithMessage("At least one child is required");

        RuleForEach(x => x.Children).ChildRules(child =>
        {
            child.RuleFor(c => c.FirstName)
                .NotEmpty().WithMessage("Child first name is required")
                .MaximumLength(100).WithMessage("Child first name cannot exceed 100 characters");

            child.RuleFor(c => c.LastName)
                .MaximumLength(100).WithMessage("Child last name cannot exceed 100 characters")
                .When(c => !string.IsNullOrEmpty(c.LastName));

            child.RuleFor(c => c.BirthDate)
                .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("Child birth date cannot be in the future")
                .When(c => c.BirthDate.HasValue);
        });
    }
}
