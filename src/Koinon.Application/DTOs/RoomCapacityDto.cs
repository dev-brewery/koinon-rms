namespace Koinon.Application.DTOs;

/// <summary>
/// Detailed capacity information for a room/location.
/// </summary>
public record RoomCapacityDto
{
    /// <summary>
    /// IdKey of the location.
    /// </summary>
    public required string IdKey { get; init; }

    /// <summary>
    /// Name of the location.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Soft capacity threshold (warning level).
    /// </summary>
    public int? SoftCapacity { get; init; }

    /// <summary>
    /// Hard capacity limit (cannot exceed without override).
    /// </summary>
    public int? HardCapacity { get; init; }

    /// <summary>
    /// Current attendance count.
    /// </summary>
    public required int CurrentCount { get; init; }

    /// <summary>
    /// Current capacity status.
    /// </summary>
    public required CapacityStatus CapacityStatus { get; init; }

    /// <summary>
    /// Percentage of soft capacity used (0-100+).
    /// </summary>
    public int PercentageFull { get; init; }

    /// <summary>
    /// Staff-to-child ratio requirement (e.g., 1 staff per 5 children = 5).
    /// </summary>
    public int? StaffToChildRatio { get; init; }

    /// <summary>
    /// Current number of staff checked in.
    /// </summary>
    public int CurrentStaffCount { get; init; }

    /// <summary>
    /// Number of staff required based on current attendance and ratio.
    /// </summary>
    public int? RequiredStaffCount { get; init; }

    /// <summary>
    /// Indicates whether the location meets staff ratio requirements.
    /// </summary>
    public bool MeetsStaffRatio { get; init; }

    /// <summary>
    /// Overflow location IdKey.
    /// </summary>
    public string? OverflowLocationIdKey { get; init; }

    /// <summary>
    /// Overflow location name.
    /// </summary>
    public string? OverflowLocationName { get; init; }

    /// <summary>
    /// Indicates whether overflow assignment should be automatic.
    /// </summary>
    public bool AutoAssignOverflow { get; init; }

    /// <summary>
    /// Indicates whether this location is active.
    /// </summary>
    public required bool IsActive { get; init; }
}

/// <summary>
/// Request to update capacity settings for a location.
/// </summary>
public record UpdateCapacitySettingsDto
{
    /// <summary>
    /// Soft capacity threshold (warning level).
    /// </summary>
    public int? SoftCapacity { get; init; }

    /// <summary>
    /// Hard capacity limit (cannot exceed without override).
    /// </summary>
    public int? HardCapacity { get; init; }

    /// <summary>
    /// Staff-to-child ratio requirement.
    /// </summary>
    public int? StaffToChildRatio { get; init; }

    /// <summary>
    /// Overflow location IdKey.
    /// </summary>
    public string? OverflowLocationIdKey { get; init; }

    /// <summary>
    /// Indicates whether overflow assignment should be automatic.
    /// </summary>
    public bool AutoAssignOverflow { get; init; }
}

/// <summary>
/// Request to override capacity for a specific check-in.
/// </summary>
public record CapacityOverrideRequestDto
{
    /// <summary>
    /// Location IdKey to override capacity for.
    /// </summary>
    public required string LocationIdKey { get; init; }

    /// <summary>
    /// Supervisor PIN for authorization.
    /// </summary>
    public required string SupervisorPin { get; init; }

    /// <summary>
    /// Reason for the override.
    /// </summary>
    public required string Reason { get; init; }
}
