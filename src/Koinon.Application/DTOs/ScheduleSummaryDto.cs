namespace Koinon.Application.DTOs;

/// <summary>
/// Summary schedule DTO for lists and references.
/// </summary>
public record ScheduleSummaryDto
{
    public required string IdKey { get; init; }
    public required Guid Guid { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public DayOfWeek? WeeklyDayOfWeek { get; init; }
    public TimeSpan? WeeklyTimeOfDay { get; init; }
    public required bool IsActive { get; init; }
}

/// <summary>
/// Schedule occurrence DTO representing a specific instance of a schedule.
/// </summary>
public record ScheduleOccurrenceDto
{
    public required DateTime OccurrenceDateTime { get; init; }
    public required string DayOfWeekName { get; init; }
    public required string FormattedTime { get; init; }
    public DateTime? CheckInWindowStart { get; init; }
    public DateTime? CheckInWindowEnd { get; init; }
    public required bool IsCheckInWindowOpen { get; init; }
}
