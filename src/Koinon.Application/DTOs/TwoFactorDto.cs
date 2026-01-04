namespace Koinon.Application.DTOs;

/// <summary>
/// DTO containing two-factor authentication setup information.
/// Includes the secret key, QR code URI, and recovery codes.
/// </summary>
public record TwoFactorSetupDto
{
    public required string SecretKey { get; init; }
    public required string QrCodeUri { get; init; }
    public required IReadOnlyList<string> RecoveryCodes { get; init; }
}

/// <summary>
/// DTO representing the current two-factor authentication status for a user.
/// </summary>
public record TwoFactorStatusDto
{
    public required bool IsEnabled { get; init; }
    public DateTime? EnabledAt { get; init; }
}
