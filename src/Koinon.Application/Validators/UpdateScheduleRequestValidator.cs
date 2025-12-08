using FluentValidation;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for UpdateScheduleRequest.
/// </summary>
public class UpdateScheduleRequestValidator : AbstractValidator<UpdateScheduleRequest>
{
    public UpdateScheduleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Schedule name cannot be empty")
            .MaximumLength(50).WithMessage("Schedule name cannot exceed 50 characters")
            .When(x => x.Name != null);

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(x => x.Description != null);

        RuleFor(x => x.WeeklyDayOfWeek)
            .IsInEnum().WithMessage("Invalid day of week")
            .When(x => x.WeeklyDayOfWeek.HasValue);

        RuleFor(x => x.WeeklyTimeOfDay)
            .Must(t => t >= TimeSpan.Zero && t < TimeSpan.FromDays(1))
            .WithMessage("Time of day must be between 00:00:00 and 23:59:59")
            .When(x => x.WeeklyTimeOfDay.HasValue);

        RuleFor(x => x.CheckInStartOffsetMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Check-in start offset must be 0 or greater")
            .When(x => x.CheckInStartOffsetMinutes.HasValue);

        RuleFor(x => x.CheckInEndOffsetMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Check-in end offset must be 0 or greater")
            .When(x => x.CheckInEndOffsetMinutes.HasValue);

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0).WithMessage("Order must be 0 or greater")
            .When(x => x.Order.HasValue);

        // Effective date range validation
        RuleFor(x => x)
            .Must(x => !x.EffectiveStartDate.HasValue || !x.EffectiveEndDate.HasValue ||
                      x.EffectiveStartDate.Value <= x.EffectiveEndDate.Value)
            .WithMessage("Effective start date must be before or equal to effective end date")
            .When(x => x.EffectiveStartDate.HasValue && x.EffectiveEndDate.HasValue);
    }
}
