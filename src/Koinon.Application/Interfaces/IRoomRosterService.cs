using Koinon.Application.DTOs;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service interface for room roster operations.
/// Provides real-time roster views for teachers and volunteers to see who is in their classroom.
/// </summary>
public interface IRoomRosterService
{
    /// <summary>
    /// Gets the current roster for a specific location/room.
    /// Shows all children currently checked in with relevant details for teachers.
    /// </summary>
    /// <param name="locationIdKey">IdKey of the location (group)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Complete room roster with all checked-in children</returns>
    Task<RoomRosterDto> GetRoomRosterAsync(string locationIdKey, CancellationToken ct = default);

    /// <summary>
    /// Gets the roster for multiple locations at once.
    /// Used by supervisors to see rosters across all rooms.
    /// </summary>
    /// <param name="locationIdKeys">List of location IdKeys</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of room rosters</returns>
    Task<IReadOnlyList<RoomRosterDto>> GetMultipleRoomRostersAsync(
        IEnumerable<string> locationIdKeys,
        CancellationToken ct = default);

    /// <summary>
    /// Checks out a child from the roster view.
    /// Convenience method for quick checkout from roster display.
    /// </summary>
    /// <param name="attendanceIdKey">IdKey of the attendance record</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if checkout was successful, false otherwise</returns>
    Task<bool> CheckOutFromRosterAsync(string attendanceIdKey, CancellationToken ct = default);
}
