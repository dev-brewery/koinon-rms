using Koinon.Domain.Enums;

namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a request by a person to join a group.
/// Tracks the request lifecycle from submission through approval or denial by group leaders.
/// </summary>
public class GroupMemberRequest : Entity
{
    /// <summary>
    /// Foreign key to the Group being requested.
    /// </summary>
    public required int GroupId { get; set; }

    /// <summary>
    /// Navigation property to the Group.
    /// </summary>
    public virtual Group? Group { get; set; }

    /// <summary>
    /// Foreign key to the Person requesting to join the group.
    /// </summary>
    public required int PersonId { get; set; }

    /// <summary>
    /// Navigation property to the requesting Person.
    /// </summary>
    public virtual Person? Person { get; set; }

    /// <summary>
    /// Current status of the request (Pending, Approved, or Denied).
    /// </summary>
    public GroupMemberRequestStatus Status { get; set; } = GroupMemberRequestStatus.Pending;

    /// <summary>
    /// Optional message from the requester explaining why they want to join.
    /// </summary>
    public string? RequestNote { get; set; }

    /// <summary>
    /// Optional message from the approver/denier providing feedback or explanation.
    /// </summary>
    public string? ResponseNote { get; set; }

    /// <summary>
    /// Foreign key to the Person who processed (approved or denied) the request.
    /// Null if the request has not yet been processed.
    /// </summary>
    public int? ProcessedByPersonId { get; set; }

    /// <summary>
    /// Navigation property to the Person who processed the request.
    /// </summary>
    public virtual Person? ProcessedByPerson { get; set; }

    /// <summary>
    /// Date and time when the request was processed (approved or denied).
    /// Null if the request is still pending.
    /// </summary>
    public DateTime? ProcessedDateTime { get; set; }
}
