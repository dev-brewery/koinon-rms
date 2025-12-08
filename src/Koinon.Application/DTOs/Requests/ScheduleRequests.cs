namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to create a new schedule.
/// </summary>
public record CreateScheduleRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }

    // Weekly schedule configuration (required for MVP)
    public DayOfWeek? WeeklyDayOfWeek { get; init; }
    public TimeSpan? WeeklyTimeOfDay { get; init; }

    // Check-in window configuration
    public int? CheckInStartOffsetMinutes { get; init; }
    public int? CheckInEndOffsetMinutes { get; init; }

    // Effective date range
    public DateOnly? EffectiveStartDate { get; init; }
    public DateOnly? EffectiveEndDate { get; init; }

    // Display and visibility
    public bool IsActive { get; init; } = true;
    public bool IsPublic { get; init; } = false;
    public int Order { get; init; } = 0;

    // Advanced features (stubbed for MVP)
    public string? ICalendarContent { get; init; }
    public bool AutoInactivateWhenComplete { get; init; } = false;
}

/// <summary>
/// Request to update an existing schedule.
/// </summary>
public record UpdateScheduleRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }

    // Weekly schedule configuration
    public DayOfWeek? WeeklyDayOfWeek { get; init; }
    public TimeSpan? WeeklyTimeOfDay { get; init; }

    // Check-in window configuration
    public int? CheckInStartOffsetMinutes { get; init; }
    public int? CheckInEndOffsetMinutes { get; init; }

    // Effective date range
    public DateOnly? EffectiveStartDate { get; init; }
    public DateOnly? EffectiveEndDate { get; init; }

    // Display and visibility
    public bool? IsActive { get; init; }
    public bool? IsPublic { get; init; }
    public int? Order { get; init; }

    // Advanced features
    public string? ICalendarContent { get; init; }
    public bool? AutoInactivateWhenComplete { get; init; }
}

/// <summary>
/// Search parameters for schedules.
/// </summary>
public record ScheduleSearchParameters
{
    public string? Query { get; init; }
    public bool IncludeInactive { get; init; } = false;
    public DayOfWeek? DayOfWeek { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}
