using Koinon.Application.DTOs;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service interface for check-in attendance operations.
/// Handles recording attendance, generating security codes, and managing check-in/check-out.
/// Performance target: &lt;200ms for individual check-in, &lt;500ms for batch check-in.
/// </summary>
public interface ICheckinAttendanceService
{
    /// <summary>
    /// Checks in a single person to a location.
    /// Creates an Attendance record and optionally generates a security code.
    /// </summary>
    /// <param name="request">Check-in request with person, location, and options</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Check-in result with attendance details and security code</returns>
    Task<CheckinResultDto> CheckInAsync(CheckinRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Checks in multiple family members at once.
    /// Optimized for batch operations to minimize database round-trips.
    /// </summary>
    /// <param name="request">Batch check-in request with multiple people</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Batch result with individual results for each person</returns>
    Task<BatchCheckinResultDto> BatchCheckInAsync(BatchCheckinRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Checks out a person from a location.
    /// Sets the EndDateTime on the attendance record.
    /// </summary>
    /// <param name="attendanceIdKey">IdKey of the attendance record</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if check-out was successful, false if attendance not found</returns>
    Task<bool> CheckOutAsync(string attendanceIdKey, CancellationToken ct = default);

    /// <summary>
    /// Gets all people currently checked in to a location.
    /// Only returns attendance records without an EndDateTime.
    /// </summary>
    /// <param name="locationIdKey">IdKey of the location (group)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of current attendance records</returns>
    Task<IReadOnlyList<AttendanceSummaryDto>> GetCurrentAttendanceAsync(
        string locationIdKey,
        CancellationToken ct = default);

    /// <summary>
    /// Gets attendance history for a person.
    /// Returns recent attendance records ordered by most recent first.
    /// </summary>
    /// <param name="personIdKey">IdKey of the person</param>
    /// <param name="days">Number of days to look back (default 30)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of historical attendance records</returns>
    Task<IReadOnlyList<AttendanceSummaryDto>> GetPersonAttendanceHistoryAsync(
        string personIdKey,
        int days = 30,
        CancellationToken ct = default);

    /// <summary>
    /// Validates whether a person can check in to a location.
    /// Checks for duplicate check-ins, capacity limits, and schedule windows.
    /// </summary>
    /// <param name="personIdKey">IdKey of the person</param>
    /// <param name="locationIdKey">IdKey of the location</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Validation result indicating if check-in is allowed</returns>
    Task<CheckinValidationResult> ValidateCheckinAsync(
        string personIdKey,
        string locationIdKey,
        CancellationToken ct = default);
}
