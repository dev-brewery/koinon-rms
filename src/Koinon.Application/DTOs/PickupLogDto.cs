namespace Koinon.Application.DTOs;

/// <summary>
/// DTO representing a pickup log entry for audit trail.
/// </summary>
public record PickupLogDto(
    string IdKey,
    string AttendanceIdKey,
    string ChildName,
    string PickupPersonName,
    bool WasAuthorized,
    bool SupervisorOverride,
    string? SupervisorName,
    DateTime CheckoutDateTime,
    string? Notes
);
