namespace Koinon.Application.DTOs;

/// <summary>
/// Check-in configuration for a kiosk or campus.
/// Contains all settings needed to run check-in operations.
/// </summary>
public record CheckinConfigurationDto
{
    /// <summary>
    /// Campus information for this check-in configuration.
    /// </summary>
    public required CampusSummaryDto Campus { get; init; }

    /// <summary>
    /// Available check-in areas (groups) at this location.
    /// </summary>
    public required IReadOnlyList<CheckinAreaDto> Areas { get; init; }

    /// <summary>
    /// Active schedules for today.
    /// </summary>
    public required IReadOnlyList<ScheduleDto> ActiveSchedules { get; init; }

    /// <summary>
    /// Current server time (for clock synchronization).
    /// </summary>
    public required DateTime ServerTime { get; init; }
}

/// <summary>
/// Represents a check-in area (special group type for children's ministry, volunteers, etc.).
/// </summary>
public record CheckinAreaDto
{
    /// <summary>
    /// IdKey of the group representing this check-in area.
    /// </summary>
    public required string IdKey { get; init; }

    /// <summary>
    /// Globally unique identifier.
    /// </summary>
    public required Guid Guid { get; init; }

    /// <summary>
    /// Name of the check-in area.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Group type information.
    /// </summary>
    public required GroupTypeSummaryDto GroupType { get; init; }

    /// <summary>
    /// Available locations within this check-in area.
    /// </summary>
    public required IReadOnlyList<CheckinLocationDto> Locations { get; init; }

    /// <summary>
    /// Schedule for when this area is open.
    /// </summary>
    public ScheduleDto? Schedule { get; init; }

    /// <summary>
    /// Indicates whether this area is currently active.
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// Current capacity status.
    /// </summary>
    public required CapacityStatus CapacityStatus { get; init; }

    /// <summary>
    /// Minimum age in months for eligibility in this area (null = no restriction).
    /// </summary>
    public int? MinAgeMonths { get; init; }

    /// <summary>
    /// Maximum age in months for eligibility in this area (null = no restriction).
    /// </summary>
    public int? MaxAgeMonths { get; init; }

    /// <summary>
    /// Minimum grade for eligibility (-1 = Pre-K, 0 = K, 1+ = grades). Null = no restriction.
    /// </summary>
    public int? MinGrade { get; init; }

    /// <summary>
    /// Maximum grade for eligibility (-1 = Pre-K, 0 = K, 1+ = grades). Null = no restriction.
    /// </summary>
    public int? MaxGrade { get; init; }
}

/// <summary>
/// Represents a location within a check-in area.
/// </summary>
public record CheckinLocationDto
{
    /// <summary>
    /// IdKey of the location.
    /// </summary>
    public required string IdKey { get; init; }

    /// <summary>
    /// Name of the location.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Full path of location (Building > Floor > Room).
    /// </summary>
    public required string FullPath { get; init; }

    /// <summary>
    /// Soft capacity threshold (warning level).
    /// </summary>
    public int? SoftCapacity { get; init; }

    /// <summary>
    /// Hard capacity threshold (cannot exceed).
    /// </summary>
    public int? HardCapacity { get; init; }

    /// <summary>
    /// Current attendance count.
    /// </summary>
    public required int CurrentCount { get; init; }

    /// <summary>
    /// Current capacity status.
    /// </summary>
    public required CapacityStatus CapacityStatus { get; init; }

    /// <summary>
    /// Indicates whether this location is active.
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// IdKey of the printer device for this location.
    /// </summary>
    public string? PrinterDeviceIdKey { get; init; }

    /// <summary>
    /// Percentage of soft capacity used (0-100+).
    /// </summary>
    public int PercentageFull { get; init; }

    /// <summary>
    /// Overflow location IdKey when this room is full.
    /// </summary>
    public string? OverflowLocationIdKey { get; init; }

    /// <summary>
    /// Overflow location name.
    /// </summary>
    public string? OverflowLocationName { get; init; }

    /// <summary>
    /// Indicates whether overflow assignment should be automatic.
    /// </summary>
    public bool AutoAssignOverflow { get; init; }
}

/// <summary>
/// Schedule information for check-in.
/// </summary>
public record ScheduleDto
{
    /// <summary>
    /// IdKey of the schedule.
    /// </summary>
    public required string IdKey { get; init; }

    /// <summary>
    /// Globally unique identifier.
    /// </summary>
    public required Guid Guid { get; init; }

    /// <summary>
    /// Name of the schedule.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Day of week (for simple weekly schedules).
    /// </summary>
    public DayOfWeek? WeeklyDayOfWeek { get; init; }

    /// <summary>
    /// Time of day (for simple weekly schedules).
    /// </summary>
    public TimeSpan? WeeklyTimeOfDay { get; init; }

    /// <summary>
    /// Minutes before scheduled time when check-in opens.
    /// </summary>
    public int? CheckInStartOffsetMinutes { get; init; }

    /// <summary>
    /// Minutes after scheduled time when check-in closes.
    /// </summary>
    public int? CheckInEndOffsetMinutes { get; init; }

    /// <summary>
    /// Indicates whether this schedule is currently active.
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// Indicates whether check-in is currently open for this schedule.
    /// </summary>
    public required bool IsCheckinActive { get; init; }

    /// <summary>
    /// Date and time when check-in opens.
    /// </summary>
    public DateTime? CheckinStartTime { get; init; }

    /// <summary>
    /// Date and time when check-in closes.
    /// </summary>
    public DateTime? CheckinEndTime { get; init; }

    /// <summary>
    /// Indicates whether this schedule is visible in public calendars.
    /// </summary>
    public bool IsPublic { get; init; }

    /// <summary>
    /// Display order for sorting schedules.
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    /// The date when this schedule becomes effective.
    /// </summary>
    public DateOnly? EffectiveStartDate { get; init; }

    /// <summary>
    /// The date when this schedule is no longer effective.
    /// </summary>
    public DateOnly? EffectiveEndDate { get; init; }

    /// <summary>
    /// iCalendar content string (RRULE) for complex recurrence patterns.
    /// </summary>
    public string? ICalendarContent { get; init; }

    /// <summary>
    /// Indicates whether this schedule should be automatically deactivated when complete.
    /// </summary>
    public bool AutoInactivateWhenComplete { get; init; }

    /// <summary>
    /// Date and time when this schedule was created.
    /// </summary>
    public DateTime CreatedDateTime { get; init; }

    /// <summary>
    /// Date and time when this schedule was last modified.
    /// </summary>
    public DateTime? ModifiedDateTime { get; init; }
}

/// <summary>
/// Capacity status for locations and areas.
/// </summary>
public enum CapacityStatus
{
    /// <summary>
    /// Below soft capacity threshold.
    /// </summary>
    Available = 0,

    /// <summary>
    /// At or above soft capacity threshold.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// At or above hard capacity threshold.
    /// </summary>
    Full = 2
}
