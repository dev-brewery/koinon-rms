namespace Koinon.Application.DTOs;

/// <summary>
/// DTO representing an active user session for security monitoring.
/// </summary>
public record UserSessionDto
{
    public required string IdKey { get; init; }
    public string? DeviceInfo { get; init; }
    public required string IpAddress { get; init; }
    public string? Location { get; init; }
    public required DateTime LastActivityAt { get; init; }
    public required bool IsActive { get; init; }
    public required bool IsCurrentSession { get; init; }
    public required DateTime CreatedDateTime { get; init; }
}
