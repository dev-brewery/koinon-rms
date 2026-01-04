using Koinon.Application.DTOs.Auth;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for authentication operations including login, token refresh, and logout.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user with email and password, returning JWT tokens.
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <param name="ipAddress">IP address of the client making the request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Token response with access token, refresh token, and user info</returns>
    Task<TokenResponse?> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default);

    /// <summary>
    /// Generates a new access token using a valid refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token value</param>
    /// <param name="ipAddress">IP address of the client making the request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>New token response or null if refresh token is invalid</returns>
    Task<TokenResponse?> RefreshTokenAsync(string refreshToken, string? ipAddress, CancellationToken ct = default);

    /// <summary>
    /// Revokes a refresh token, preventing it from being used again.
    /// </summary>
    /// <param name="refreshToken">The refresh token to revoke</param>
    /// <param name="ipAddress">IP address of the client making the request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if token was revoked, false if token was not found</returns>
    Task<bool> LogoutAsync(string refreshToken, string? ipAddress, CancellationToken ct = default);

    /// <summary>
    /// Hashes a password using Argon2id for secure storage.
    /// </summary>
    /// <param name="password">The plaintext password to hash</param>
    /// <returns>Base64-encoded hash with embedded salt</returns>
    Task<string> HashPasswordAsync(string password);

    /// <summary>
    /// Verifies a password against a stored hash.
    /// </summary>
    /// <param name="password">The plaintext password to verify</param>
    /// <param name="storedHash">The stored password hash (Base64-encoded with embedded salt)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if password matches, false otherwise</returns>
    Task<bool> VerifyPasswordAsync(string password, string storedHash, CancellationToken cancellationToken = default);
}
