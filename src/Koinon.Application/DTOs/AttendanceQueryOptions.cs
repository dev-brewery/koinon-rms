namespace Koinon.Application.DTOs;

/// <summary>
/// Query parameters for filtering and grouping attendance data.
/// </summary>
public record AttendanceQueryOptions(
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    string? CampusIdKey = null,
    string? GroupTypeIdKey = null,
    string? GroupIdKey = null,
    GroupBy GroupBy = GroupBy.Day
);

/// <summary>
/// Enumeration for time-based grouping of attendance data.
/// </summary>
public enum GroupBy
{
    Day,
    Week,
    Month,
    Year
}
