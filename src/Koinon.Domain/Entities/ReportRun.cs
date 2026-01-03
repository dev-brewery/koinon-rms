using Koinon.Domain.Enums;

namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a single execution of a report generation job.
/// Tracks progress, status, output, and errors for report runs.
/// </summary>
public class ReportRun : Entity
{
    /// <summary>
    /// Foreign key to the report definition being executed.
    /// </summary>
    public int ReportDefinitionId { get; set; }

    /// <summary>
    /// Current status of the report generation job.
    /// </summary>
    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    /// <summary>
    /// JSON structure containing the parameters used for this report run.
    /// Format: {"startDate": "2024-01-01", "endDate": "2024-12-31", "campusId": 5}
    /// </summary>
    public required string Parameters { get; set; }

    /// <summary>
    /// Foreign key to the generated output file (BinaryFile).
    /// Null until report generation completes successfully.
    /// </summary>
    public int? OutputFileId { get; set; }

    /// <summary>
    /// Timestamp when the report generation started.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Timestamp when the report generation completed (success or failure).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Error message if the report generation failed.
    /// Null if the report completed successfully.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Foreign key to the PersonAlias who requested this report.
    /// Null for system-triggered or scheduled reports.
    /// </summary>
    public int? RequestedByPersonAliasId { get; set; }

    // Navigation properties

    /// <summary>
    /// Navigation property to the report definition being executed.
    /// </summary>
    public virtual ReportDefinition ReportDefinition { get; set; } = null!;

    /// <summary>
    /// Navigation property to the generated output file.
    /// </summary>
    public virtual BinaryFile? OutputFile { get; set; }

    /// <summary>
    /// Navigation property to the person who requested this report.
    /// </summary>
    public virtual PersonAlias? RequestedByPersonAlias { get; set; }
}
