using FluentValidation;
using Koinon.Application.DTOs.VolunteerSchedule;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for CreateScheduleAssignmentsRequest.
/// Ensures required fields are present and dates are not in the past.
/// </summary>
public class CreateScheduleAssignmentsRequestValidator : AbstractValidator<CreateScheduleAssignmentsRequest>
{
    public CreateScheduleAssignmentsRequestValidator()
    {
        RuleFor(x => x.MemberIdKeys)
            .NotEmpty()
            .WithMessage("At least one member is required");

        RuleFor(x => x.ScheduleIdKey)
            .NotEmpty()
            .WithMessage("Schedule is required");

        RuleFor(x => x.Dates)
            .NotEmpty()
            .WithMessage("At least one date is required");

        RuleForEach(x => x.Dates)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Cannot schedule for past dates");
    }
}
