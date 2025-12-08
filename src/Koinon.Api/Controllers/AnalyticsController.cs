using Koinon.Api.Filters;
using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for attendance analytics and reporting.
/// Provides endpoints for summary statistics, trends, and group-based breakdowns.
/// </summary>
[ApiController]
[Route("api/v1/analytics")]
[Authorize]
[ValidateIdKey]
public class AnalyticsController(
    IAttendanceAnalyticsService analyticsService,
    ILogger<AnalyticsController> logger) : ControllerBase
{
    /// <summary>
    /// Gets summary analytics for attendance over a date range.
    /// </summary>
    /// <param name="startDate">Start date for the analysis period (ISO 8601 format)</param>
    /// <param name="endDate">End date for the analysis period (ISO 8601 format)</param>
    /// <param name="campusIdKey">Optional campus IdKey to filter results</param>
    /// <param name="groupTypeIdKey">Optional group type IdKey to filter results</param>
    /// <param name="groupIdKey">Optional group IdKey to filter results</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Summary analytics including total attendance, unique attendees, and averages</returns>
    /// <response code="200">Returns attendance summary statistics</response>
    /// <response code="400">Invalid IdKey format</response>
    [HttpGet("attendance")]
    [ProducesResponseType(typeof(AttendanceAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAttendanceSummary(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] string? campusIdKey,
        [FromQuery] string? groupTypeIdKey,
        [FromQuery] string? groupIdKey,
        CancellationToken ct = default)
    {
        var options = new AttendanceQueryOptions(
            StartDate: startDate,
            EndDate: endDate,
            CampusIdKey: campusIdKey,
            GroupTypeIdKey: groupTypeIdKey,
            GroupIdKey: groupIdKey
        );

        var analytics = await analyticsService.GetSummaryAsync(options, ct);

        logger.LogInformation(
            "Attendance summary retrieved: StartDate={StartDate}, EndDate={EndDate}, TotalAttendance={TotalAttendance}, UniqueAttendees={UniqueAttendees}",
            analytics.StartDate, analytics.EndDate, analytics.TotalAttendance, analytics.UniqueAttendees);

        return Ok(analytics);
    }

    /// <summary>
    /// Gets attendance trends over time with flexible grouping.
    /// </summary>
    /// <param name="startDate">Start date for the analysis period (ISO 8601 format)</param>
    /// <param name="endDate">End date for the analysis period (ISO 8601 format)</param>
    /// <param name="campusIdKey">Optional campus IdKey to filter results</param>
    /// <param name="groupTypeIdKey">Optional group type IdKey to filter results</param>
    /// <param name="groupIdKey">Optional group IdKey to filter results</param>
    /// <param name="groupBy">Time grouping: Day, Week, Month, or Year (default: Day)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of time-series data points with attendance counts</returns>
    /// <response code="200">Returns attendance trend data</response>
    /// <response code="400">Invalid IdKey format or groupBy value</response>
    [HttpGet("attendance/trends")]
    [ProducesResponseType(typeof(IReadOnlyList<AttendanceTrendDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAttendanceTrends(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] string? campusIdKey,
        [FromQuery] string? groupTypeIdKey,
        [FromQuery] string? groupIdKey,
        [FromQuery] GroupBy groupBy = GroupBy.Day,
        CancellationToken ct = default)
    {
        var options = new AttendanceQueryOptions(
            StartDate: startDate,
            EndDate: endDate,
            CampusIdKey: campusIdKey,
            GroupTypeIdKey: groupTypeIdKey,
            GroupIdKey: groupIdKey,
            GroupBy: groupBy
        );

        var trends = await analyticsService.GetTrendsAsync(options, ct);

        logger.LogInformation(
            "Attendance trends retrieved: StartDate={StartDate}, EndDate={EndDate}, GroupBy={GroupBy}, DataPoints={DataPoints}",
            startDate, endDate, groupBy, trends.Count);

        return Ok(trends);
    }

    /// <summary>
    /// Gets attendance statistics broken down by group.
    /// </summary>
    /// <param name="startDate">Start date for the analysis period (ISO 8601 format)</param>
    /// <param name="endDate">End date for the analysis period (ISO 8601 format)</param>
    /// <param name="campusIdKey">Optional campus IdKey to filter results</param>
    /// <param name="groupTypeIdKey">Optional group type IdKey to filter results</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of attendance metrics per group</returns>
    /// <response code="200">Returns attendance data by group</response>
    /// <response code="400">Invalid IdKey format</response>
    [HttpGet("attendance/by-group")]
    [ProducesResponseType(typeof(IReadOnlyList<AttendanceByGroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAttendanceByGroup(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] string? campusIdKey,
        [FromQuery] string? groupTypeIdKey,
        CancellationToken ct = default)
    {
        var options = new AttendanceQueryOptions(
            StartDate: startDate,
            EndDate: endDate,
            CampusIdKey: campusIdKey,
            GroupTypeIdKey: groupTypeIdKey
        );

        var byGroup = await analyticsService.GetByGroupAsync(options, ct);

        logger.LogInformation(
            "Attendance by group retrieved: StartDate={StartDate}, EndDate={EndDate}, GroupCount={GroupCount}",
            startDate, endDate, byGroup.Count);

        return Ok(byGroup);
    }
}
