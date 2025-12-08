using Koinon.Application.DTOs;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for generating attendance analytics and reports.
/// Provides summary statistics, trends, and group-based breakdowns.
/// </summary>
public interface IAttendanceAnalyticsService
{
    /// <summary>
    /// Gets summary analytics for attendance over a date range.
    /// </summary>
    /// <param name="options">Query parameters for filtering attendance data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Summary analytics including total attendance, unique attendees, and averages.</returns>
    Task<AttendanceAnalyticsDto> GetSummaryAsync(
        AttendanceQueryOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets attendance trends over time (grouped by day, week, month, or year).
    /// </summary>
    /// <param name="options">Query parameters for filtering and grouping attendance data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of time-series data points with attendance counts.</returns>
    Task<IReadOnlyList<AttendanceTrendDto>> GetTrendsAsync(
        AttendanceQueryOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets attendance statistics broken down by group.
    /// </summary>
    /// <param name="options">Query parameters for filtering attendance data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of attendance metrics per group.</returns>
    Task<IReadOnlyList<AttendanceByGroupDto>> GetByGroupAsync(
        AttendanceQueryOptions options,
        CancellationToken cancellationToken = default);
}
