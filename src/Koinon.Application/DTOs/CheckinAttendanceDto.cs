namespace Koinon.Application.DTOs;

/// <summary>
/// Request to check in a single person.
/// </summary>
public record CheckinRequestDto
{
    /// <summary>
    /// IdKey of the person to check in.
    /// </summary>
    public required string PersonIdKey { get; init; }

    /// <summary>
    /// IdKey of the location (group) to check in to.
    /// </summary>
    public required string LocationIdKey { get; init; }

    /// <summary>
    /// IdKey of the schedule for this check-in (optional).
    /// </summary>
    public string? ScheduleIdKey { get; init; }

    /// <summary>
    /// Date of the occurrence (defaults to today).
    /// </summary>
    public DateOnly? OccurrenceDate { get; init; }

    /// <summary>
    /// IdKey of the device (kiosk) performing the check-in (optional).
    /// </summary>
    public string? DeviceIdKey { get; init; }

    /// <summary>
    /// Whether to generate a security code for this check-in.
    /// Typically true for children, false for adults.
    /// </summary>
    public bool GenerateSecurityCode { get; init; }

    /// <summary>
    /// Optional notes about this check-in.
    /// </summary>
    public string? Note { get; init; }
}

/// <summary>
/// Request to check in multiple people at once (family batch check-in).
/// </summary>
public record BatchCheckinRequestDto(
    List<CheckinRequestDto> CheckIns,
    string? DeviceIdKey = null);

/// <summary>
/// Result of a single check-in operation.
/// </summary>
public record CheckinResultDto(
    bool Success,
    string? ErrorMessage = null,
    string? AttendanceIdKey = null,
    string? SecurityCode = null,
    DateTime? CheckInTime = null,
    CheckinPersonSummaryDto? Person = null,
    CheckinLocationSummaryDto? Location = null);

/// <summary>
/// Result of a batch check-in operation.
/// </summary>
public record BatchCheckinResultDto(
    List<CheckinResultDto> Results,
    int SuccessCount,
    int FailureCount,
    bool AllSucceeded);

/// <summary>
/// Summary of attendance for display purposes.
/// </summary>
public record AttendanceSummaryDto(
    string IdKey,
    CheckinPersonSummaryDto Person,
    CheckinLocationSummaryDto Location,
    DateTime StartDateTime,
    DateTime? EndDateTime = null,
    string? SecurityCode = null,
    bool IsFirstTime = false,
    string? Note = null);

/// <summary>
/// Minimal person summary for check-in operations.
/// </summary>
public record CheckinPersonSummaryDto(
    string IdKey,
    string FullName,
    string FirstName,
    string LastName,
    string? NickName = null,
    int? Age = null,
    string? PhotoUrl = null);

/// <summary>
/// Minimal location summary for check-in operations.
/// </summary>
public record CheckinLocationSummaryDto(
    string IdKey,
    string Name,
    string FullPath);

/// <summary>
/// Result of check-in validation.
/// </summary>
public record CheckinValidationResult(
    bool IsAllowed,
    string? Reason = null,
    bool IsAlreadyCheckedIn = false,
    bool IsAtCapacity = false,
    bool IsOutsideSchedule = false);
