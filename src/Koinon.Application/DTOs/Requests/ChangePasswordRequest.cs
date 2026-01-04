namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to change the current user's password.
/// Requires current password for verification and new password confirmation.
/// </summary>
public record ChangePasswordRequest
{
    public required string CurrentPassword { get; init; }
    public required string NewPassword { get; init; }
    public required string ConfirmPassword { get; init; }
}
