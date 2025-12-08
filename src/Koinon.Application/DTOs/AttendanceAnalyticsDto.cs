namespace Koinon.Application.DTOs;

/// <summary>
/// Summary analytics for attendance over a date range.
/// Provides key metrics for attendance reporting and dashboard displays.
/// </summary>
public record AttendanceAnalyticsDto(
    int TotalAttendance,
    int UniqueAttendees,
    int FirstTimeVisitors,
    int ReturningVisitors,
    decimal AverageAttendance,
    DateOnly StartDate,
    DateOnly EndDate
);
