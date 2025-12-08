namespace Koinon.Application.DTOs;

/// <summary>
/// Time-series data point for attendance trends.
/// Represents attendance counts for a specific date or time period.
/// </summary>
public record AttendanceTrendDto(
    DateOnly Date,
    int Count,
    int FirstTime,
    int Returning
);
