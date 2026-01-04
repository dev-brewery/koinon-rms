using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for managing user settings including preferences, sessions, and two-factor authentication.
/// </summary>
public interface IUserSettingsService
{
    /// <summary>
    /// Gets the user preferences for the specified person.
    /// </summary>
    Task<Result<UserPreferenceDto>> GetPreferencesAsync(int personId, CancellationToken ct = default);

    /// <summary>
    /// Updates the user preferences for the specified person.
    /// </summary>
    Task<Result<UserPreferenceDto>> UpdatePreferencesAsync(
        int personId,
        UpdateUserPreferenceRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all active sessions for the specified person.
    /// </summary>
    Task<Result<IReadOnlyList<UserSessionDto>>> GetSessionsAsync(int personId, CancellationToken ct = default);

    /// <summary>
    /// Revokes a specific session for the specified person.
    /// </summary>
    Task<Result> RevokeSessionAsync(int personId, string sessionIdKey, CancellationToken ct = default);

    /// <summary>
    /// Changes the password for the specified person.
    /// </summary>
    Task<Result> ChangePasswordAsync(
        int personId,
        ChangePasswordRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the two-factor authentication status for the specified person.
    /// </summary>
    Task<Result<TwoFactorStatusDto>> GetTwoFactorStatusAsync(int personId, CancellationToken ct = default);

    /// <summary>
    /// Sets up two-factor authentication for the specified person.
    /// Returns the secret key, QR code URI, and recovery codes.
    /// </summary>
    Task<Result<TwoFactorSetupDto>> SetupTwoFactorAsync(int personId, CancellationToken ct = default);

    /// <summary>
    /// Verifies a two-factor authentication code and enables 2FA for the specified person.
    /// </summary>
    Task<Result> VerifyTwoFactorAsync(
        int personId,
        TwoFactorVerifyRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Disables two-factor authentication for the specified person after verification.
    /// </summary>
    Task<Result> DisableTwoFactorAsync(
        int personId,
        TwoFactorVerifyRequest request,
        CancellationToken ct = default);
}
