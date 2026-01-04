using Koinon.Domain.Enums;

namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a single execution of a CSV import operation.
/// Tracks progress, status, and errors for import jobs.
/// </summary>
public class ImportJob : Entity
{
    /// <summary>
    /// Foreign key to the template used for this import (nullable - ad-hoc imports may not use a template).
    /// </summary>
    public int? ImportTemplateId { get; set; }

    /// <summary>
    /// Type of data being imported.
    /// </summary>
    public ImportType ImportType { get; set; }

    /// <summary>
    /// Current status of the import job.
    /// </summary>
    public ImportJobStatus Status { get; set; } = ImportJobStatus.Pending;

    /// <summary>
    /// Original filename of the uploaded CSV file.
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// Total number of rows in the CSV file (excluding header).
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// Number of rows that have been processed so far.
    /// </summary>
    public int ProcessedRows { get; set; }

    /// <summary>
    /// Number of rows that were successfully imported.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of rows that failed validation or import.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// JSON structure containing row-level error details.
    /// Format: {"errors": [{"row": 5, "column": "Email", "value": "invalid", "message": "Invalid email format"}]}
    /// </summary>
    public string? ErrorDetails { get; set; }

    /// <summary>
    /// Timestamp when the job started processing.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Timestamp when the job completed (success, failure, or cancellation).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Storage key for the uploaded CSV file (used for background job processing).
    /// Null for synchronous imports or after file cleanup.
    /// </summary>
    public string? StorageKey { get; set; }

    /// <summary>
    /// Hangfire job ID for tracking background job execution.
    /// Null for synchronous imports.
    /// </summary>
    public string? BackgroundJobId { get; set; }

    // Navigation properties

    /// <summary>
    /// Navigation property to the template used for this import.
    /// </summary>
    public virtual ImportTemplate? ImportTemplate { get; set; }


}
