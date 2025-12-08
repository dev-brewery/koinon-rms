namespace Koinon.Application.DTOs;

/// <summary>
/// Attendance metrics grouped by a specific group.
/// Provides breakdown of attendance counts by group for reporting.
/// </summary>
public record AttendanceByGroupDto(
    string GroupIdKey,
    string GroupName,
    string GroupTypeName,
    int TotalAttendance,
    int UniqueAttendees
);
