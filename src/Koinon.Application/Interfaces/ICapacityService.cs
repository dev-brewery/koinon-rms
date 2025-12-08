using Koinon.Application.DTOs;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service interface for room capacity management operations.
/// Handles capacity tracking, overflow management, and staff ratio enforcement.
/// </summary>
public interface ICapacityService
{
    /// <summary>
    /// Gets detailed capacity information for a location.
    /// </summary>
    /// <param name="locationIdKey">IdKey of the location</param>
    /// <param name="occurrenceDate">Date of the occurrence (defaults to today)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Capacity information or null if location not found</returns>
    Task<RoomCapacityDto?> GetLocationCapacityAsync(
        string locationIdKey,
        DateOnly? occurrenceDate = null,
        CancellationToken ct = default);

    /// <summary>
    /// Updates capacity settings for a location.
    /// </summary>
    /// <param name="locationIdKey">IdKey of the location</param>
    /// <param name="settings">Capacity settings to update</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if successful, false if location not found</returns>
    Task<bool> UpdateCapacitySettingsAsync(
        string locationIdKey,
        UpdateCapacitySettingsDto settings,
        CancellationToken ct = default);

    /// <summary>
    /// Validates whether a location can accept additional check-ins.
    /// </summary>
    /// <param name="locationIdKey">IdKey of the location</param>
    /// <param name="occurrenceDate">Date of the occurrence (defaults to today)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if location has capacity, false if full</returns>
    Task<bool> CanAcceptCheckinAsync(
        string locationIdKey,
        DateOnly? occurrenceDate = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the recommended overflow location when primary location is full.
    /// </summary>
    /// <param name="locationIdKey">IdKey of the primary location</param>
    /// <param name="occurrenceDate">Date of the occurrence (defaults to today)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Overflow location capacity info or null if no overflow configured or overflow also full</returns>
    Task<RoomCapacityDto?> GetOverflowLocationAsync(
        string locationIdKey,
        DateOnly? occurrenceDate = null,
        CancellationToken ct = default);

    /// <summary>
    /// Validates staff-to-child ratio for a location.
    /// </summary>
    /// <param name="locationIdKey">IdKey of the location</param>
    /// <param name="occurrenceDate">Date of the occurrence (defaults to today)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if ratio is met or not configured, false if insufficient staff</returns>
    Task<bool> ValidateStaffRatioAsync(
        string locationIdKey,
        DateOnly? occurrenceDate = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets capacity status for multiple locations at once.
    /// </summary>
    /// <param name="locationIdKeys">Array of location IdKeys</param>
    /// <param name="occurrenceDate">Date of the occurrence (defaults to today)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of capacity information for each location</returns>
    Task<IReadOnlyList<RoomCapacityDto>> GetMultipleLocationCapacitiesAsync(
        IEnumerable<string> locationIdKeys,
        DateOnly? occurrenceDate = null,
        CancellationToken ct = default);
}
