namespace Koinon.Domain.Enums;

/// <summary>
/// Types of actions that can be performed on financial entities for audit logging.
/// </summary>
public enum FinancialAuditAction
{
    /// <summary>
    /// View action - reading financial data.
    /// </summary>
    View = 0,

    /// <summary>
    /// Create action - creating new financial records.
    /// </summary>
    Create = 1,

    /// <summary>
    /// Update action - modifying existing financial records.
    /// </summary>
    Update = 2,

    /// <summary>
    /// Delete action - removing financial records.
    /// </summary>
    Delete = 3,

    /// <summary>
    /// Export action - exporting financial data.
    /// </summary>
    Export = 4,

    /// <summary>
    /// Print action - printing financial reports or records.
    /// </summary>
    Print = 5
}
