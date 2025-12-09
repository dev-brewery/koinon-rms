using FluentValidation;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for UpdateMyProfileRequest.
/// Ensures email format and phone number formats are valid.
/// </summary>
public class UpdateMyProfileRequestValidator : AbstractValidator<UpdateMyProfileRequest>
{
    public UpdateMyProfileRequestValidator()
    {
        When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
        {
            RuleFor(x => x.Email)
                .EmailAddress()
                .WithMessage("Email must be a valid email address");
        });

        When(x => !string.IsNullOrWhiteSpace(x.EmailPreference), () =>
        {
            RuleFor(x => x.EmailPreference)
                .Must(ep => ep == "EmailAllowed" || ep == "NoMassEmails" || ep == "DoNotEmail")
                .WithMessage("EmailPreference must be EmailAllowed, NoMassEmails, or DoNotEmail");
        });

        When(x => !string.IsNullOrWhiteSpace(x.NickName), () =>
        {
            RuleFor(x => x.NickName)
                .MaximumLength(50)
                .WithMessage("NickName cannot exceed 50 characters");
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
}

/// <summary>
/// Validator for UpdatePhoneNumberRequest.
/// </summary>
public class UpdatePhoneNumberRequestValidator : AbstractValidator<UpdatePhoneNumberRequest>
{
    public UpdatePhoneNumberRequestValidator()
    {
        RuleFor(x => x.Number)
            .NotEmpty()
            .WithMessage("Phone number is required")
            .Matches(@"^\d{10,15}$")
            .WithMessage("Phone number must be 10-15 digits");

        When(x => !string.IsNullOrWhiteSpace(x.Extension), () =>
        {
            RuleFor(x => x.Extension)
                .MaximumLength(20)
                .WithMessage("Extension cannot exceed 20 characters");
        });
    }
}
