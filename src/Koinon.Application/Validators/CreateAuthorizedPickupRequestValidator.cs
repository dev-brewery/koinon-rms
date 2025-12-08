using FluentValidation;
using Koinon.Application.DTOs.Requests;
using Koinon.Domain.Enums;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for CreateAuthorizedPickupRequest.
/// </summary>
public class CreateAuthorizedPickupRequestValidator : AbstractValidator<CreateAuthorizedPickupRequest>
{
    public CreateAuthorizedPickupRequestValidator()
    {
        // Either AuthorizedPersonIdKey OR Name must be provided (not both empty)
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.AuthorizedPersonIdKey) ||
                      !string.IsNullOrWhiteSpace(x.Name))
            .WithMessage("Either AuthorizedPersonIdKey or Name must be provided");

        // If Name provided, max length 200
        RuleFor(x => x.Name)
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Name));

        // If PhoneNumber provided, validate E.164 format
        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .WithMessage("Phone number must be in valid E.164 format (e.g., +12345678901)")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        // Relationship must be valid enum value
        RuleFor(x => x.Relationship)
            .IsInEnum()
            .WithMessage("Relationship must be a valid value");

        // AuthorizationLevel must be valid enum value
        RuleFor(x => x.AuthorizationLevel)
            .IsInEnum()
            .WithMessage("AuthorizationLevel must be a valid value");

        // PhotoUrl max length 500 if provided
        RuleFor(x => x.PhotoUrl)
            .MaximumLength(500).WithMessage("PhotoUrl cannot exceed 500 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.PhotoUrl));

        // CustodyNotes max length 2000 if provided
        RuleFor(x => x.CustodyNotes)
            .MaximumLength(2000).WithMessage("CustodyNotes cannot exceed 2000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.CustodyNotes));
    }
}
