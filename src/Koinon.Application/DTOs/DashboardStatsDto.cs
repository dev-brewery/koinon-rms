namespace Koinon.Application.DTOs;

/// <summary>
/// Dashboard statistics summary data transfer object.
/// Provides key metrics for the admin dashboard overview.
/// </summary>
public record DashboardStatsDto
{
    /// <summary>
    /// Total count of active people in the system.
    /// </summary>
    public required int TotalPeople { get; init; }

    /// <summary>
    /// Total count of family groups.
    /// </summary>
    public required int TotalFamilies { get; init; }

    /// <summary>
    /// Count of active non-family groups.
    /// </summary>
    public required int ActiveGroups { get; init; }

    /// <summary>
    /// Count of check-ins for today.
    /// </summary>
    public required int TodayCheckIns { get; init; }

    /// <summary>
    /// Count of check-ins for the same day last week (for comparison).
    /// </summary>
    public required int LastWeekCheckIns { get; init; }

    /// <summary>
    /// Count of active schedules.
    /// </summary>
    public required int ActiveSchedules { get; init; }

    /// <summary>
    /// List of upcoming schedules with their next occurrence times.
    /// </summary>
    public required List<UpcomingScheduleDto> UpcomingSchedules { get; init; }
}

/// <summary>
/// Data transfer object for upcoming schedule information.
/// </summary>
public record UpcomingScheduleDto
{
    /// <summary>
    /// URL-safe identifier for the schedule.
    /// </summary>
    public required string IdKey { get; init; }

    /// <summary>
    /// Name of the schedule.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The date and time of the next occurrence.
    /// </summary>
    public required DateTime NextOccurrence { get; init; }

    /// <summary>
    /// Minutes until check-in opens for this schedule.
    /// Negative values indicate check-in is already open.
    /// </summary>
    public required int MinutesUntilCheckIn { get; init; }
}
