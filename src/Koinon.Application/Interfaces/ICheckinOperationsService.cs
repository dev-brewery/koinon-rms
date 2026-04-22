using Koinon.Application.Common;
using Koinon.Application.DTOs.CheckinOperations;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Orchestration service for the live check-in operations dashboard (#482).
/// Provides a consolidated view of rooms, attendees, and summary stats for
/// coordinators during Sunday morning operations.
/// </summary>
public interface ICheckinOperationsService
{
    /// <summary>
    /// Gets the live dashboard payload: rooms with capacity pills, flat attendee list,
    /// and summary counts. Intended to be polled every 5 seconds from the client.
    /// </summary>
    Task<Result<CheckinOperationsDashboardDto>> GetDashboardAsync(CancellationToken ct = default);

    /// <summary>
    /// Toggles a room's open/closed state. Coordinator-only.
    /// </summary>
    Task<Result<ToggleRoomResponseDto>> ToggleRoomAsync(string locationIdKey, CancellationToken ct = default);
}
