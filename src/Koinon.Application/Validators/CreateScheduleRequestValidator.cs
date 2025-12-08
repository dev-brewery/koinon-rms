using FluentValidation;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for CreateScheduleRequest.
/// </summary>
public class CreateScheduleRequestValidator : AbstractValidator<CreateScheduleRequest>
{
    public CreateScheduleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Schedule name is required")
            .MaximumLength(50).WithMessage("Schedule name cannot exceed 50 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        // For MVP, require weekly schedule configuration
        RuleFor(x => x.WeeklyDayOfWeek)
            .NotNull().WithMessage("Day of week is required for weekly schedules")
            .IsInEnum().WithMessage("Invalid day of week");

        RuleFor(x => x.WeeklyTimeOfDay)
            .NotNull().WithMessage("Time of day is required for weekly schedules")
            .Must(t => t >= TimeSpan.Zero && t < TimeSpan.FromDays(1))
            .WithMessage("Time of day must be between 00:00:00 and 23:59:59");

        // Check-in offset validation
        RuleFor(x => x.CheckInStartOffsetMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Check-in start offset must be 0 or greater")
            .When(x => x.CheckInStartOffsetMinutes.HasValue);

        RuleFor(x => x.CheckInEndOffsetMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Check-in end offset must be 0 or greater")
            .When(x => x.CheckInEndOffsetMinutes.HasValue);

        // Effective date range validation
        RuleFor(x => x)
            .Must(x => !x.EffectiveStartDate.HasValue || !x.EffectiveEndDate.HasValue ||
                      x.EffectiveStartDate.Value <= x.EffectiveEndDate.Value)
            .WithMessage("Effective start date must be before or equal to effective end date")
            .When(x => x.EffectiveStartDate.HasValue && x.EffectiveEndDate.HasValue);

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0).WithMessage("Order must be 0 or greater");
    }
}
