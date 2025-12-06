namespace Koinon.Domain.Enums;

/// <summary>
/// Represents the status of a person's membership in a group.
/// </summary>
public enum GroupMemberStatus
{
    /// <summary>
    /// The person is no longer an active member of the group.
    /// </summary>
    Inactive = 0,

    /// <summary>
    /// The person is an active member of the group.
    /// </summary>
    Active = 1,

    /// <summary>
    /// The person's membership is pending approval.
    /// </summary>
    Pending = 2
}
