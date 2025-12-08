namespace Koinon.Domain.Enums;

/// <summary>
/// Defines the level of authorization for a person to pick up a child.
/// </summary>
public enum AuthorizationLevel
{
    /// <summary>
    /// Person is always authorized to pick up the child.
    /// </summary>
    Always = 0,

    /// <summary>
    /// Person is only authorized in emergency situations, requires supervisor approval.
    /// </summary>
    EmergencyOnly = 1,

    /// <summary>
    /// Person is explicitly blocked from picking up the child (custody situations).
    /// </summary>
    Never = 2
}
