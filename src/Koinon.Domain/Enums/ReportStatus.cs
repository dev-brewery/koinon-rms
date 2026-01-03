namespace Koinon.Domain.Enums;

/// <summary>
/// Represents the execution status of a report generation job.
/// </summary>
public enum ReportStatus
{
    /// <summary>
    /// Report is queued and waiting to be processed.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Report is currently being generated.
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Report generation completed successfully.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Report generation failed due to errors.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Report generation was cancelled by user or system.
    /// </summary>
    Cancelled = 4
}
