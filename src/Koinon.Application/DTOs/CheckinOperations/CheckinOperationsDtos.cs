namespace Koinon.Application.DTOs.CheckinOperations;

/// <summary>
/// Represents a single checked-in attendee row for the operations dashboard.
/// Flat shape so the frontend can filter client-side across all attendees.
/// </summary>
public record CheckinOperationsAttendeeDto(
    string AttendanceIdKey,
    string PersonIdKey,
    string FullName,
    string LocationIdKey,
    string LocationName,
    DateTime CheckInTime,
    DateTime? CheckOutTime,
    bool IsPresent,
    string? Allergies = null,
    bool HasCriticalAllergies = false,
    string? SecurityCode = null,
    bool IsFirstTime = false);

/// <summary>
/// Represents a single room (location) on the operations dashboard.
/// </summary>
public record CheckinOperationsRoomDto(
    string LocationIdKey,
    string LocationName,
    int CheckedInCount,
    int? Capacity,
    int PercentFull,
    string CapacityPillColor,
    bool IsOpen);

/// <summary>
/// Summary stats card for the operations dashboard.
/// </summary>
public record CheckinOperationsSummaryDto(
    int TotalCheckedIn,
    int CurrentlyPresent,
    int CheckedOut);

/// <summary>
/// Full dashboard response: rooms, attendees, and summary.
/// </summary>
public record CheckinOperationsDashboardDto(
    IReadOnlyList<CheckinOperationsRoomDto> Rooms,
    IReadOnlyList<CheckinOperationsAttendeeDto> Attendees,
    CheckinOperationsSummaryDto Summary,
    DateTime GeneratedAt);

/// <summary>
/// Response from a room toggle operation.
/// </summary>
public record ToggleRoomResponseDto(
    string LocationIdKey,
    bool IsOpen);
