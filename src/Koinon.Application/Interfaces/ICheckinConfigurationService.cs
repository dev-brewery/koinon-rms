using Koinon.Application.DTOs;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service interface for check-in configuration operations.
/// Manages check-in area configurations, schedules, and location capacity tracking.
/// </summary>
public interface ICheckinConfigurationService
{
    /// <summary>
    /// Gets the complete check-in configuration for a specific campus.
    /// Includes all active check-in areas, locations, and schedules.
    /// </summary>
    /// <param name="campusIdKey">IdKey of the campus</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Check-in configuration or null if campus not found</returns>
    Task<CheckinConfigurationDto?> GetConfigurationByCampusAsync(
        string campusIdKey,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the check-in configuration for a specific kiosk device.
    /// Returns configuration scoped to the device's assigned campus and areas.
    /// </summary>
    /// <param name="deviceIdKey">IdKey of the kiosk device</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Check-in configuration or null if device not found</returns>
    Task<CheckinConfigurationDto?> GetConfigurationByKioskAsync(
        string deviceIdKey,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all check-in areas that are currently open for check-in at a campus.
    /// Filters based on schedule and time windows.
    /// </summary>
    /// <param name="campusIdKey">IdKey of the campus</param>
    /// <param name="currentTime">Current date/time (optional, defaults to DateTime.UtcNow)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of active check-in areas</returns>
    Task<IReadOnlyList<CheckinAreaDto>> GetActiveAreasAsync(
        string campusIdKey,
        DateTime? currentTime = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets detailed information about a specific check-in area.
    /// </summary>
    /// <param name="areaIdKey">IdKey of the check-in area (group)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Check-in area details or null if not found</returns>
    Task<CheckinAreaDto?> GetAreaByIdKeyAsync(
        string areaIdKey,
        CancellationToken ct = default);

    /// <summary>
    /// Gets current capacity information for a location.
    /// </summary>
    /// <param name="locationIdKey">IdKey of the location</param>
    /// <param name="occurrenceDate">Date of the occurrence (defaults to today)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Location with current capacity or null if not found</returns>
    Task<CheckinLocationDto?> GetLocationCapacityAsync(
        string locationIdKey,
        DateOnly? occurrenceDate = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all schedules that are active for check-in at a specific campus.
    /// </summary>
    /// <param name="campusIdKey">IdKey of the campus</param>
    /// <param name="currentTime">Current date/time (optional, defaults to DateTime.UtcNow)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of active schedules</returns>
    Task<IReadOnlyList<ScheduleDto>> GetActiveSchedulesAsync(
        string campusIdKey,
        DateTime? currentTime = null,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if check-in is currently open for a specific schedule.
    /// </summary>
    /// <param name="scheduleIdKey">IdKey of the schedule</param>
    /// <param name="currentTime">Current date/time (optional, defaults to DateTime.UtcNow)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if check-in is open, false otherwise</returns>
    Task<bool> IsCheckinOpenAsync(
        string scheduleIdKey,
        DateTime? currentTime = null,
        CancellationToken ct = default);
}
