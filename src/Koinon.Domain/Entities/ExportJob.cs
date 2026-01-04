using Koinon.Domain.Enums;

namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a data export job for extracting entity data to external formats.
/// Tracks the export process, parameters, output, and execution status.
/// </summary>
public class ExportJob : Entity
{
    /// <summary>
    /// Type of data being exported (People, Families, Groups, etc.).
    /// </summary>
    public ExportType ExportType { get; set; }

    /// <summary>
    /// Specific entity name for custom exports.
    /// Only populated when ExportType is Custom.
    /// Maximum length: 100 characters.
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Current status of the export job.
    /// </summary>
    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    /// <summary>
    /// JSON structure containing filter criteria and field selections for this export.
    /// Format: {"filters": {"campus": [1,2]}, "fields": ["FirstName", "LastName", "Email"]}
    /// </summary>
    public required string Parameters { get; set; }

    /// <summary>
    /// Format of the exported data file.
    /// </summary>
    public ReportOutputFormat OutputFormat { get; set; }

    /// <summary>
    /// Foreign key to the generated export file (BinaryFile).
    /// Null until export completes successfully.
    /// </summary>
    public int? OutputFileId { get; set; }

    /// <summary>
    /// Foreign key to the PersonAlias who requested this export.
    /// Null for system-triggered exports.
    /// </summary>
    public int? RequestedByPersonAliasId { get; set; }

    /// <summary>
    /// Timestamp when the export processing started.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Timestamp when the export processing completed (success or failure).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Error message if the export failed.
    /// Null if the export completed successfully.
    /// Maximum length: 2000 characters.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of records included in the export.
    /// Null until export completes.
    /// </summary>
    public int? RecordCount { get; set; }

    // Navigation properties

    /// <summary>
    /// Navigation property to the generated export file.
    /// </summary>
    public virtual BinaryFile? OutputFile { get; set; }

    /// <summary>
    /// Navigation property to the person who requested this export.
    /// </summary>
    public virtual PersonAlias? RequestedByPersonAlias { get; set; }
}
