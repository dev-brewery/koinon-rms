namespace Koinon.Application.DTOs;

/// <summary>
/// DTO representing a person's membership in a group (excludes family groups).
/// </summary>
public record PersonGroupMembershipDto
{
    public required string IdKey { get; init; }
    public required Guid Guid { get; init; }
    public required string GroupIdKey { get; init; }
    public required string GroupName { get; init; }
    public required string GroupTypeIdKey { get; init; }
    public required string GroupTypeName { get; init; }
    public required string RoleIdKey { get; init; }
    public required string RoleName { get; init; }
    public required string MemberStatus { get; init; }
    public DateTime CreatedDateTime { get; init; }
    public DateTime? ModifiedDateTime { get; init; }
}
