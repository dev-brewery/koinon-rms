using Koinon.Domain.Enums;

namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to update the status of a follow-up task.
/// </summary>
public record UpdateFollowUpStatusRequest
{
    /// <summary>
    /// The new status for the follow-up.
    /// </summary>
    public FollowUpStatus Status { get; init; }

    /// <summary>
    /// Optional notes about the status change.
    /// </summary>
    public string? Notes { get; init; }
}

/// <summary>
/// Request to assign a follow-up task to a person.
/// </summary>
public record AssignFollowUpRequest
{
    /// <summary>
    /// The IdKey of the person to assign the follow-up task to.
    /// </summary>
    public required string AssignedToIdKey { get; init; }
}
