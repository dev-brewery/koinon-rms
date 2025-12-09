using FluentValidation;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for RecordAttendanceRequest.
/// </summary>
public class RecordAttendanceRequestValidator : AbstractValidator<RecordAttendanceRequest>
{
    public RecordAttendanceRequestValidator()
    {
        RuleFor(x => x.OccurrenceDate)
            .NotEmpty().WithMessage("Occurrence date is required");

        RuleFor(x => x.AttendedPersonIds)
            .NotNull().WithMessage("Attended person IDs list is required")
            .NotEmpty().WithMessage("At least one attendee must be recorded");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes cannot exceed 2000 characters")
            .When(x => x.Notes != null);
    }
}
