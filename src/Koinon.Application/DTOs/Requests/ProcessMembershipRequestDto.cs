namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to process (approve or deny) a membership request.
/// </summary>
public record ProcessMembershipRequestDto
{
    /// <summary>
    /// Status to set for the request. Must be either "Approved" or "Denied".
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Optional note from the approver/denier providing feedback or explanation.
    /// </summary>
    public string? Note { get; init; }
}
