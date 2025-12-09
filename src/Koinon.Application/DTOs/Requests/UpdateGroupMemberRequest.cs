namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to update a group member's role or status.
/// </summary>
public record UpdateGroupMemberRequest
{
    /// <summary>
    /// Optional new role IdKey for the member.
    /// </summary>
    public string? RoleId { get; init; }

    /// <summary>
    /// Optional new status (Active, Inactive, Pending).
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Optional note about the member.
    /// </summary>
    public string? Note { get; init; }
}
