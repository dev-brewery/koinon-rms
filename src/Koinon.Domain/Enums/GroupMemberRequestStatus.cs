namespace Koinon.Domain.Enums;

/// <summary>
/// Status of a group membership request.
/// </summary>
public enum GroupMemberRequestStatus
{
    /// <summary>
    /// Request is pending review by a group leader or administrator.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Request has been approved and the person can be added to the group.
    /// </summary>
    Approved = 1,

    /// <summary>
    /// Request has been denied by a group leader or administrator.
    /// </summary>
    Denied = 2
}
