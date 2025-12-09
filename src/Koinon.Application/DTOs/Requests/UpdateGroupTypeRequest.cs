namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to update an existing group type.
/// </summary>
public record UpdateGroupTypeRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? IconCssClass { get; init; }
    public string? Color { get; init; }
    public string? GroupTerm { get; init; }
    public string? GroupMemberTerm { get; init; }
    public bool? TakesAttendance { get; init; }
    public bool? AllowSelfRegistration { get; init; }
    public bool? RequiresMemberApproval { get; init; }
    public bool? DefaultIsPublic { get; init; }
    public int? DefaultGroupCapacity { get; init; }
    public bool? ShowInGroupList { get; init; }
    public bool? ShowInNavigation { get; init; }
    public int? Order { get; init; }
}
