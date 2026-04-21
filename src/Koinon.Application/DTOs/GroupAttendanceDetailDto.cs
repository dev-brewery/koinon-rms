namespace Koinon.Application.DTOs;

/// <summary>
/// Attendance record for a single person within a specific group occurrence.
/// </summary>
public record GroupAttendanceDetailDto(
    string PersonIdKey,
    string FullName,
    string? PhotoUrl,
    bool DidAttend,
    DateTime? PresentDateTime);
