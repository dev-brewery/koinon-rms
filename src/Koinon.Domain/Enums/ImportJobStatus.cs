namespace Koinon.Domain.Enums;

/// <summary>
/// Status of an import job execution.
/// </summary>
public enum ImportJobStatus
{
    /// <summary>
    /// Job is pending execution.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Job is currently processing.
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Job completed successfully.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Job failed due to errors.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Job was cancelled by user or system.
    /// </summary>
    Cancelled = 4
}
