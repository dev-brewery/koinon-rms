using Koinon.Application.DTOs;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for supervisor mode operations in check-in kiosks.
/// Provides PIN-based authentication and session management for supervisor overrides.
/// </summary>
public interface ISupervisorModeService
{
    /// <summary>
    /// Authenticates a supervisor using their PIN code.
    /// Creates a time-limited session token for supervisor operations.
    /// </summary>
    /// <param name="request">PIN authentication request</param>
    /// <param name="ipAddress">IP address of the kiosk making the request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Login response with session token, or null if authentication failed</returns>
    Task<SupervisorLoginResponse?> LoginAsync(SupervisorLoginRequest request, string? ipAddress, CancellationToken ct = default);

    /// <summary>
    /// Validates a supervisor session token.
    /// </summary>
    /// <param name="sessionToken">Session token to validate</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Supervisor info if valid, null if invalid or expired</returns>
    Task<SupervisorInfoDto?> ValidateSessionAsync(string sessionToken, CancellationToken ct = default);

    /// <summary>
    /// Ends a supervisor session.
    /// </summary>
    /// <param name="sessionToken">Session token to invalidate</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if session was ended, false if session not found</returns>
    Task<bool> LogoutAsync(string sessionToken, CancellationToken ct = default);

    /// <summary>
    /// Records an audit log entry for a supervisor action.
    /// </summary>
    /// <param name="sessionToken">Session token of the supervisor</param>
    /// <param name="action">Description of the action performed</param>
    /// <param name="entityType">Type of entity affected (e.g., "Attendance")</param>
    /// <param name="entityIdKey">IdKey of the entity affected</param>
    /// <param name="ct">Cancellation token</param>
    Task LogActionAsync(string sessionToken, string action, string? entityType = null, string? entityIdKey = null, CancellationToken ct = default);
}
