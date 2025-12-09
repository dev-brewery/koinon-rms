namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to submit a membership request to join a group.
/// </summary>
public record SubmitMembershipRequestDto
{
    /// <summary>
    /// Optional note from the requester explaining why they want to join the group.
    /// </summary>
    public string? Note { get; init; }
}
