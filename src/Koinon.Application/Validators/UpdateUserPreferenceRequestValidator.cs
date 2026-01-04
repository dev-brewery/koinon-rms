using FluentValidation;
using Koinon.Application.DTOs.Requests;
using Koinon.Domain.Enums;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for UpdateUserPreferenceRequest.
/// Ensures date format and timezone are valid.
/// </summary>
public class UpdateUserPreferenceRequestValidator : AbstractValidator<UpdateUserPreferenceRequest>
{
    public UpdateUserPreferenceRequestValidator()
    {
        RuleFor(x => x.Theme)
            .IsInEnum()
            .WithMessage("Theme must be a valid value (System, Light, or Dark)");

        RuleFor(x => x.DateFormat)
            .NotEmpty()
            .WithMessage("Date format is required")
            .MaximumLength(20)
            .WithMessage("Date format cannot exceed 20 characters")
            .Must(BeValidDateFormat)
            .WithMessage("Date format is not a valid .NET date format pattern");

        RuleFor(x => x.TimeZone)
            .NotEmpty()
            .WithMessage("Time zone is required")
            .MaximumLength(64)
            .WithMessage("Time zone cannot exceed 64 characters")
            .Must(BeValidTimeZone)
            .WithMessage("Time zone is not a valid IANA timezone identifier");
    }

    private static bool BeValidDateFormat(string format)
    {
        try
        {
            _ = DateTime.Now.ToString(format);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool BeValidTimeZone(string timeZone)
    {
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
