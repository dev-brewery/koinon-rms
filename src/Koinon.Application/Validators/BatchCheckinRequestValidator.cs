using FluentValidation;
using Koinon.Application.DTOs;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for BatchCheckinRequestDto.
/// </summary>
public class BatchCheckinRequestValidator : AbstractValidator<BatchCheckinRequestDto>
{
    public BatchCheckinRequestValidator()
    {
        RuleFor(x => x.CheckIns)
            .NotNull().WithMessage("CheckIns collection is required")
            .NotEmpty().WithMessage("At least one check-in is required")
            .Must(checkIns => checkIns.Count <= 20)
            .WithMessage("Cannot check in more than 20 people at once");

        RuleForEach(x => x.CheckIns)
            .SetValidator(new CheckinRequestDtoValidator());

        RuleFor(x => x.DeviceIdKey)
            .Matches(@"^[A-Za-z0-9_-]{10,30}$")
            .WithMessage("DeviceIdKey must be a valid IdKey format")
            .When(x => !string.IsNullOrWhiteSpace(x.DeviceIdKey));
    }
}

/// <summary>
/// Validator for individual CheckinRequestDto.
/// </summary>
public class CheckinRequestDtoValidator : AbstractValidator<CheckinRequestDto>
{
    public CheckinRequestDtoValidator()
    {
        RuleFor(x => x.PersonIdKey)
            .NotEmpty().WithMessage("PersonIdKey is required")
            .Matches(@"^[A-Za-z0-9_-]{10,30}$")
            .WithMessage("PersonIdKey must be a valid IdKey format");

        RuleFor(x => x.LocationIdKey)
            .NotEmpty().WithMessage("LocationIdKey is required")
            .Matches(@"^[A-Za-z0-9_-]{10,30}$")
            .WithMessage("LocationIdKey must be a valid IdKey format");

        RuleFor(x => x.ScheduleIdKey)
            .Matches(@"^[A-Za-z0-9_-]{10,30}$")
            .WithMessage("ScheduleIdKey must be a valid IdKey format")
            .When(x => !string.IsNullOrWhiteSpace(x.ScheduleIdKey));

        RuleFor(x => x.DeviceIdKey)
            .Matches(@"^[A-Za-z0-9_-]{10,30}$")
            .WithMessage("DeviceIdKey must be a valid IdKey format")
            .When(x => !string.IsNullOrWhiteSpace(x.DeviceIdKey));

        RuleFor(x => x.OccurrenceDate)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today.AddDays(7)))
            .WithMessage("Occurrence date cannot be more than 7 days in the future")
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today.AddDays(-30)))
            .WithMessage("Occurrence date cannot be more than 30 days in the past")
            .When(x => x.OccurrenceDate.HasValue);

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Note cannot exceed 500 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Note));
    }
}
