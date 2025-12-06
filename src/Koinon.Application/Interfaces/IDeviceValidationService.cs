namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for validating kiosk/device tokens.
/// Used by KioskAuthorizeAttribute to authenticate kiosk check-in requests.
/// </summary>
public interface IDeviceValidationService
{
    /// <summary>
    /// Validates a kiosk token and returns the device ID if valid.
    /// </summary>
    /// <param name="token">The kiosk token from X-Kiosk-Token header</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Device ID if valid, null otherwise</returns>
    Task<int?> ValidateKioskTokenAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Revokes a kiosk token (sets device to inactive or clears token).
    /// </summary>
    /// <param name="deviceIdKey">Device IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if revoked successfully</returns>
    Task<bool> RevokeKioskTokenAsync(string deviceIdKey, CancellationToken ct = default);

    /// <summary>
    /// Generates a new kiosk token for a device.
    /// </summary>
    /// <param name="deviceIdKey">Device IdKey</param>
    /// <param name="expiresAt">Optional expiration date</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The generated token</returns>
    Task<string> GenerateKioskTokenAsync(string deviceIdKey, DateTime? expiresAt = null, CancellationToken ct = default);
}
