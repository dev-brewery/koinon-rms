namespace Koinon.Application.DTOs;

/// <summary>
/// Full device DTO with all fields including kiosk token metadata and printer settings.
/// </summary>
public record DeviceDetailDto
{
    public required string IdKey { get; init; }
    public required Guid Guid { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required bool IsActive { get; init; }

    // Device type
    public string? DeviceTypeName { get; init; }

    // Network
    public string? IpAddress { get; init; }

    // Campus relationship
    public string? CampusIdKey { get; init; }
    public string? CampusName { get; init; }

    // Printer settings (raw JSON)
    public string? PrinterSettings { get; init; }

    // Kiosk token
    public bool HasKioskToken { get; init; }
    public DateTime? KioskTokenExpiresAt { get; init; }

    // Audit fields
    public required DateTime CreatedDateTime { get; init; }
    public DateTime? ModifiedDateTime { get; init; }
}

/// <summary>
/// Response DTO returned after kiosk token generation, containing the plaintext token.
/// This is the only time the raw token is transmitted — the caller must store it.
/// </summary>
public record GenerateKioskTokenResponseDto
{
    public required string Token { get; init; }
    public required string DeviceIdKey { get; init; }
    public required string DeviceName { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

/// <summary>
/// Summary device DTO for list views.
/// </summary>
public record DeviceSummaryDto
{
    public required string IdKey { get; init; }
    public required string Name { get; init; }
    public required bool IsActive { get; init; }
    public string? DeviceTypeName { get; init; }
    public string? CampusName { get; init; }
    public string? IpAddress { get; init; }
}
