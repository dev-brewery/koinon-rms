using Koinon.Application.DTOs.Giving;

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

    /// <summary>
    /// Giving statistics summary.
    /// </summary>
    public required GivingStatsDto GivingStats { get; init; }

    /// <summary>
    /// Communications statistics summary.
    /// </summary>
    public required CommunicationsStatsDto CommunicationsStats { get; init; }
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

/// <summary>
/// Data transfer object for giving statistics.
/// </summary>
public record GivingStatsDto
{
    /// <summary>
    /// Total contributions received this month.
    /// </summary>
    public required decimal MonthToDateTotal { get; init; }

    /// <summary>
    /// Total contributions received this year.
    /// </summary>
    public required decimal YearToDateTotal { get; init; }

    /// <summary>
    /// List of recent open/pending batches (last 5).
    /// </summary>
    public required List<DashboardBatchDto> RecentBatches { get; init; }
}

/// <summary>
/// Data transfer object for batch information on the dashboard.
/// </summary>
public record DashboardBatchDto
{
    /// <summary>
    /// URL-safe identifier for the batch.
    /// </summary>
    public required string IdKey { get; init; }

    /// <summary>
    /// Name of the batch.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Date of the batch.
    /// </summary>
    public required DateTime BatchDate { get; init; }

    /// <summary>
    /// Status of the batch (e.g., Open, Pending, Closed).
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Total amount in the batch.
    /// </summary>
    public required decimal Total { get; init; }
}

/// <summary>
/// Data transfer object for communications statistics.
/// </summary>
public record CommunicationsStatsDto
{
    /// <summary>
    /// Count of communications with Pending status.
    /// </summary>
    public required int PendingCount { get; init; }

    /// <summary>
    /// Count of communications sent in the last 7 days.
    /// </summary>
    public required int SentThisWeekCount { get; init; }

    /// <summary>
    /// List of recent communications (last 5).
    /// </summary>
    public required List<CommunicationSummaryDto> RecentCommunications { get; init; }
}


