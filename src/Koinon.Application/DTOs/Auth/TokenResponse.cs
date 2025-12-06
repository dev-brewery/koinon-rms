namespace Koinon.Application.DTOs.Auth;

/// <summary>
/// Token response DTO containing JWT access token and refresh token.
/// </summary>
public record TokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    PersonSummaryDto User);
