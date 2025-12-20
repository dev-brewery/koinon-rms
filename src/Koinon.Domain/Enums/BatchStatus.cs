namespace Koinon.Domain.Enums;

/// <summary>
/// Status of a contribution batch.
/// </summary>
public enum BatchStatus
{
    /// <summary>
    /// Batch is open for adding contributions.
    /// </summary>
    Open = 0,

    /// <summary>
    /// Batch has been closed and is ready for posting.
    /// </summary>
    Closed = 1,

    /// <summary>
    /// Batch has been posted to the general ledger.
    /// </summary>
    Posted = 2
}
