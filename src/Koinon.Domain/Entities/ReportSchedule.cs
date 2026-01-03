using Koinon.Domain.Enums;

namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a scheduled recurring report generation job.
/// Manages cron-based scheduling, parameters, and recipient notifications.
/// </summary>
public class ReportSchedule : Entity
{
    /// <summary>
    /// Foreign key to the report definition to be executed on schedule.
    /// </summary>
    public int ReportDefinitionId { get; set; }

    /// <summary>
    /// Hangfire cron expression defining the schedule.
    /// Examples: "0 9 * * MON" (Mondays at 9 AM), "0 0 1 * *" (First of month at midnight).
    /// </summary>
    public required string CronExpression { get; set; }

    /// <summary>
    /// IANA timezone identifier for schedule execution.
    /// Defaults to "America/New_York".
    /// Examples: "America/New_York", "America/Chicago", "America/Los_Angeles".
    /// </summary>
    public string TimeZone { get; set; } = "America/New_York";

    /// <summary>
    /// JSON structure containing the parameters to use for scheduled report runs.
    /// Format: {"startDate": "{{TODAY-30}}", "endDate": "{{TODAY}}", "campusId": 5}
    /// Supports dynamic date expressions.
    /// </summary>
    public required string Parameters { get; set; }

    /// <summary>
    /// JSON array of PersonAlias IDs to notify when the scheduled report completes.
    /// Format: "[123, 456, 789]"
    /// </summary>
    public required string RecipientPersonAliasIds { get; set; }

    /// <summary>
    /// Output format for the generated report (PDF, Excel, CSV).
    /// </summary>
    public ReportOutputFormat OutputFormat { get; set; } = ReportOutputFormat.Pdf;

    /// <summary>
    /// Whether this schedule is currently active and should execute.
    /// Defaults to true.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Timestamp of the last successful report generation for this schedule.
    /// Null if the schedule has never run successfully.
    /// </summary>
    public DateTime? LastRunAt { get; set; }

    /// <summary>
    /// Calculated timestamp of the next scheduled report generation.
    /// Updated after each run or when the cron expression changes.
    /// </summary>
    public DateTime? NextRunAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Navigation property to the report definition being scheduled.
    /// </summary>
    public virtual ReportDefinition ReportDefinition { get; set; } = null!;
}
