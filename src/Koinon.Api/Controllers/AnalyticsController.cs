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
    IFirstTimeVisitorService firstTimeVisitorService,
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

    /// <summary>
    /// Gets all first-time visitors who checked in today.
    /// </summary>
    /// <param name="campusIdKey">Optional campus IdKey to filter results</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of first-time visitors from today</returns>
    /// <response code="200">Returns today's first-time visitors</response>
    /// <response code="400">Invalid IdKey format</response>
    [HttpGet("first-time-visitors/today")]
    [ProducesResponseType(typeof(IReadOnlyList<FirstTimeVisitorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTodaysFirstTimeVisitors(
        [FromQuery] string? campusIdKey,
        CancellationToken ct = default)
    {
        var visitors = await firstTimeVisitorService.GetTodaysFirstTimersAsync(campusIdKey, ct);

        logger.LogInformation(
            "Today's first-time visitors retrieved: CampusIdKey={CampusIdKey}, Count={Count}",
            campusIdKey ?? "All", visitors.Count);

        return Ok(visitors);
    }

    /// <summary>
    /// Gets first-time visitors who checked in within a date range.
    /// </summary>
    /// <param name="startDate">Start date for the analysis period (ISO 8601 format)</param>
    /// <param name="endDate">End date for the analysis period (ISO 8601 format)</param>
    /// <param name="campusIdKey">Optional campus IdKey to filter results</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of first-time visitors in the date range</returns>
    /// <response code="200">Returns first-time visitors in the date range</response>
    /// <response code="400">Invalid IdKey format or missing/invalid dates</response>
    [HttpGet("first-time-visitors")]
    [ProducesResponseType(typeof(IReadOnlyList<FirstTimeVisitorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetFirstTimeVisitorsByDateRange(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] string? campusIdKey,
        CancellationToken ct = default)
    {
        if (!startDate.HasValue || !endDate.HasValue)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid date range",
                Detail = "Both startDate and endDate are required.",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        if (startDate.Value > endDate.Value)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid date range",
                Detail = "startDate must be less than or equal to endDate.",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        var visitors = await firstTimeVisitorService.GetFirstTimersByDateRangeAsync(
            startDate.Value,
            endDate.Value,
            campusIdKey,
            ct);

        logger.LogInformation(
            "First-time visitors by date range retrieved: StartDate={StartDate}, EndDate={EndDate}, CampusIdKey={CampusIdKey}, Count={Count}",
            startDate, endDate, campusIdKey ?? "All", visitors.Count);

        return Ok(visitors);
    }
}
