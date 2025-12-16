namespace Koinon.Application.DTOs;

/// <summary>
/// Result of marking a single person's attendance.
/// </summary>
public record MarkAttendanceResultDto(
    bool Success,
    string? ErrorMessage = null,
    string? AttendanceIdKey = null,
    bool IsFirstTime = false,
    DateTime? PresentDateTime = null);

/// <summary>
/// Result of bulk attendance marking operation.
/// </summary>
public record BulkMarkAttendanceResultDto(
    List<MarkAttendanceResultDto> Results,
    int SuccessCount,
    int FailureCount,
    bool AllSucceeded);

/// <summary>
/// A person's entry in an occurrence roster with attendance status.
/// </summary>
public record OccurrenceRosterEntryDto(
    string PersonIdKey,
    string FullName,
    string FirstName,
    string LastName,
    string? NickName,
    int? Age,
    string? PhotoUrl,
    bool IsAttending,
    string? AttendanceIdKey = null,
    DateTime? PresentDateTime = null,
    bool IsFirstTime = false,
    string? Note = null);

/// <summary>
/// A family grouping in an occurrence roster.
/// </summary>
public record FamilyRosterGroupDto(
    string FamilyIdKey,
    string FamilyName,
    List<OccurrenceRosterEntryDto> Members,
    int AttendingCount,
    int TotalCount);
