namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to verify a two-factor authentication code.
/// </summary>
public record TwoFactorVerifyRequest
{
    public required string Code { get; init; }
}
