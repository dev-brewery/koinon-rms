namespace Koinon.Application.DTOs;

/// <summary>
/// Response DTO for check-in opportunities for a family.
/// Contains the family info plus per-member check-in options.
/// </summary>
public record CheckinOpportunitiesResponseDto
{
    public required CheckinFamilySearchResultDto Family { get; init; }
    public required IReadOnlyList<PersonOpportunitiesDto> Opportunities { get; init; }
}

/// <summary>
/// Available check-in options for a single person.
/// </summary>
public record PersonOpportunitiesDto
{
    public required CheckinFamilyMemberDto Person { get; init; }
    public required IReadOnlyList<CurrentAttendanceDto> CurrentAttendance { get; init; }
    public required IReadOnlyList<CheckinOptionDto> AvailableOptions { get; init; }
}

/// <summary>
/// A currently active attendance record (already checked in).
/// </summary>
public record CurrentAttendanceDto
{
    public required string AttendanceIdKey { get; init; }
    public required string Group { get; init; }
    public required string Location { get; init; }
    public required string Schedule { get; init; }
    public required string SecurityCode { get; init; }
    public required DateTime CheckInTime { get; init; }
    public bool CanCheckOut { get; init; } = true;
}

/// <summary>
/// A group (ministry) that a person can check into.
/// </summary>
public record CheckinOptionDto
{
    public required string GroupIdKey { get; init; }
    public required string GroupName { get; init; }
    public required IReadOnlyList<CheckinLocationOptionDto> Locations { get; init; }
}

/// <summary>
/// A location (room) within a group option.
/// </summary>
public record CheckinLocationOptionDto
{
    public required string LocationIdKey { get; init; }
    public required string LocationName { get; init; }
    public required IReadOnlyList<CheckinScheduleOptionDto> Schedules { get; init; }
    public int CurrentCount { get; init; }
    public int? SoftThreshold { get; init; }
    public int? FirmThreshold { get; init; }
}

/// <summary>
/// A schedule option within a location.
/// </summary>
public record CheckinScheduleOptionDto
{
    public required string ScheduleIdKey { get; init; }
    public required string ScheduleName { get; init; }
    public required string StartTime { get; init; }
    public bool IsSelected { get; init; }
}
