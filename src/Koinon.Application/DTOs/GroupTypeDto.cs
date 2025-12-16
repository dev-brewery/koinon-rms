namespace Koinon.Application.DTOs;

/// <summary>
/// Group type DTO for list views.
/// </summary>
public record GroupTypeDto
{
    public required string IdKey { get; init; }
    public required Guid Guid { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? IconCssClass { get; init; }
    public string? Color { get; init; }
    public required string GroupTerm { get; init; }
    public required string GroupMemberTerm { get; init; }
    public bool TakesAttendance { get; init; }
    public bool AllowSelfRegistration { get; init; }
    public bool RequiresMemberApproval { get; init; }
    public bool DefaultIsPublic { get; init; }
    public int? DefaultGroupCapacity { get; init; }
    public bool IsSystem { get; init; }
    public bool IsArchived { get; init; }
    public int Order { get; init; }
    public int GroupCount { get; init; }
}

/// <summary>
/// Detailed group type DTO with all configuration fields.
/// </summary>
public record GroupTypeDetailDto
{
    public required string IdKey { get; init; }
    public required Guid Guid { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? IconCssClass { get; init; }
    public string? Color { get; init; }
    public required string GroupTerm { get; init; }
    public required string GroupMemberTerm { get; init; }
    public bool TakesAttendance { get; init; }
    public bool AllowSelfRegistration { get; init; }
    public bool RequiresMemberApproval { get; init; }
    public bool DefaultIsPublic { get; init; }
    public int? DefaultGroupCapacity { get; init; }
    public bool ShowInGroupList { get; init; }
    public bool ShowInNavigation { get; init; }
    public bool AttendanceCountsAsWeekendService { get; init; }
    public bool SendAttendanceReminder { get; init; }
    public bool AllowMultipleLocations { get; init; }
    public bool EnableSpecificGroupRequirements { get; init; }
    public bool AllowGroupSync { get; init; }
    public bool AllowSpecificGroupMemberAttributes { get; init; }
    public bool ShowConnectionStatus { get; init; }
    public bool IgnorePersonInactivated { get; init; }
    public bool IsSystem { get; init; }
    public bool IsArchived { get; init; }
    public int Order { get; init; }
    public int GroupCount { get; init; }
    public required DateTime CreatedDateTime { get; init; }
    public DateTime? ModifiedDateTime { get; init; }
}

/// <summary>
/// Summary of a group type used in group DTOs.
/// </summary>
public record GroupTypeSummaryDto
{
    public required string IdKey { get; init; }
    public required Guid Guid { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool IsFamilyGroupType { get; init; } = false; // Deprecated: families are now separate
    public required bool AllowMultipleLocations { get; init; }
    public required IReadOnlyList<GroupTypeRoleDto> Roles { get; init; }
}
