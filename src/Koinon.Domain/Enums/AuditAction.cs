namespace Koinon.Domain.Enums;

/// <summary>
/// Types of actions that can be audited in the system.
/// </summary>
public enum AuditAction
{
    /// <summary>
    /// Create action - creating new records.
    /// </summary>
    Create = 0,

    /// <summary>
    /// Update action - modifying existing records.
    /// </summary>
    Update = 1,

    /// <summary>
    /// Delete action - removing records.
    /// </summary>
    Delete = 2,

    /// <summary>
    /// View action - reading or viewing data.
    /// </summary>
    View = 3,

    /// <summary>
    /// Export action - exporting data from the system.
    /// </summary>
    Export = 4,

    /// <summary>
    /// Login action - user authentication event.
    /// </summary>
    Login = 5,

    /// <summary>
    /// Logout action - user session termination.
    /// </summary>
    Logout = 6,

    /// <summary>
    /// Search action - performing a search query.
    /// </summary>
    Search = 7,

    /// <summary>
    /// Other action - any action not covered by specific types.
    /// </summary>
    Other = 8
}
