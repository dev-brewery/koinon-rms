namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to create a new group type.
/// </summary>
public record CreateGroupTypeRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? IconCssClass { get; init; }
    public string? Color { get; init; }
    public string GroupTerm { get; init; } = "Group";
    public string GroupMemberTerm { get; init; } = "Member";
    public bool TakesAttendance { get; init; }
    public bool AllowSelfRegistration { get; init; }
    public bool RequiresMemberApproval { get; init; } = true;
    public bool DefaultIsPublic { get; init; }
    public int? DefaultGroupCapacity { get; init; }
    public bool ShowInGroupList { get; init; }
    public bool ShowInNavigation { get; init; }
    public int Order { get; init; }
}
