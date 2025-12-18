using Koinon.Application.DTOs;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service interface for manual attendance taking operations.
/// Extends beyond kiosk check-in to support staff marking attendance during services,
/// bulk entry, and historical recording.
/// Performance target: <100ms for single mark, <500ms for family mark.
/// </summary>
public interface IAttendanceTakerService
{
    /// <summary>
    /// Marks a single person as attended for an occurrence.
    /// Creates or updates Attendance record with DidAttend=true and PresentDateTime.
    /// </summary>
    /// <param name="occurrenceIdKey">IdKey of the attendance occurrence</param>
    /// <param name="personIdKey">IdKey of the person to mark</param>
    /// <param name="note">Optional note about this attendance</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result indicating success/failure with attendance details</returns>
    Task<MarkAttendanceResultDto> MarkAttendedAsync(
        string occurrenceIdKey,
        string personIdKey,
        string? note = null,
        CancellationToken ct = default);

    /// <summary>
    /// Marks all members of a family as attended for an occurrence.
    /// Optimized batch operation to minimize database round-trips.
    /// </summary>
    /// <param name="occurrenceIdKey">IdKey of the attendance occurrence</param>
    /// <param name="familyIdKey">IdKey of the family</param>
    /// <param name="note">Optional note applied to all family members</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Batch result with individual results for each family member</returns>
    Task<BulkMarkAttendanceResultDto> MarkFamilyAttendedAsync(
        string occurrenceIdKey,
        string familyIdKey,
        string? note = null,
        CancellationToken ct = default);

    /// <summary>
    /// Removes attendance mark for a person at an occurrence.
    /// Sets DidAttend=false or removes the Attendance record if appropriate.
    /// </summary>
    /// <param name="occurrenceIdKey">IdKey of the attendance occurrence</param>
    /// <param name="personIdKey">IdKey of the person</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if successfully unmarked, false if attendance not found</returns>
    Task<bool> UnmarkAttendedAsync(
        string occurrenceIdKey,
        string personIdKey,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all people eligible to attend an occurrence with their current attendance status.
    /// Used for attendance-taking UI showing who is checked in vs. who is not.
    /// </summary>
    /// <param name="occurrenceIdKey">IdKey of the attendance occurrence</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of people with attendance status</returns>
    Task<IReadOnlyList<OccurrenceRosterEntryDto>> GetOccurrenceRosterAsync(
        string occurrenceIdKey,
        CancellationToken ct = default);

    /// <summary>
    /// Gets attendance roster grouped by family for easier bulk marking.
    /// Optionally filters by search term (name or phone).
    /// </summary>
    /// <param name="occurrenceIdKey">IdKey of the attendance occurrence</param>
    /// <param name="searchTerm">Optional search term to filter families/people</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of families with their members and attendance status</returns>
    Task<IReadOnlyList<FamilyRosterGroupDto>> GetFamilyGroupedRosterAsync(
        string occurrenceIdKey,
        string? searchTerm = null,
        CancellationToken ct = default);

    /// <summary>
    /// Marks multiple people as attended in a single operation.
    /// Optimized for bulk attendance entry scenarios.
    /// </summary>
    /// <param name="occurrenceIdKey">IdKey of the attendance occurrence</param>
    /// <param name="personIdKeys">Array of person IdKeys to mark</param>
    /// <param name="note">Optional note applied to all attendees</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Batch result with individual results for each person</returns>
    Task<BulkMarkAttendanceResultDto> BulkMarkAttendedAsync(
        string occurrenceIdKey,
        string[] personIdKeys,
        string? note = null,
        CancellationToken ct = default);
}
