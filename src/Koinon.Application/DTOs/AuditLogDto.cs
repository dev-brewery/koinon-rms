using Koinon.Domain.Enums;

namespace Koinon.Application.DTOs;

/// <summary>
/// Main DTO for audit log entries.
/// </summary>
public record AuditLogDto
{
    /// <summary>
    /// Encoded identifier for the audit log entry.
    /// </summary>
    public required string IdKey { get; init; }

    /// <summary>
    /// Type of action that was performed.
    /// </summary>
    public required AuditAction ActionType { get; init; }

    /// <summary>
    /// Type of entity that was affected (e.g., "Person", "Group").
    /// </summary>
    public required string EntityType { get; init; }

    /// <summary>
    /// Encoded identifier of the affected entity.
    /// </summary>
    public required string EntityIdKey { get; init; }

    /// <summary>
    /// Encoded identifier of the person who performed the action.
    /// </summary>
    public string? PersonIdKey { get; init; }

    /// <summary>
    /// Display name of the person who performed the action.
    /// </summary>
    public string? PersonName { get; init; }

    /// <summary>
    /// When the action occurred.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// JSON representation of entity state before the change.
    /// </summary>
    public string? OldValues { get; init; }

    /// <summary>
    /// JSON representation of entity state after the change.
    /// </summary>
    public string? NewValues { get; init; }

    /// <summary>
    /// IP address from which the action was performed.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// User agent string of the client that performed the action.
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// List of property names that were changed (parsed from JSON diff).
    /// </summary>
    public List<string>? ChangedProperties { get; init; }

    /// <summary>
    /// Additional context or metadata about the action.
    /// </summary>
    public string? AdditionalInfo { get; init; }
}

/// <summary>
/// Search and filter parameters for querying audit logs.
/// </summary>
public record AuditLogSearchParameters
{
    /// <summary>
    /// Start of the date range to search (inclusive).
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// End of the date range to search (inclusive).
    /// </summary>
    public DateTime? EndDate { get; init; }

    /// <summary>
    /// Filter by entity type (e.g., "Person", "Group").
    /// </summary>
    public string? EntityType { get; init; }

    /// <summary>
    /// Filter by action type.
    /// </summary>
    public AuditAction? ActionType { get; init; }

    /// <summary>
    /// Filter by person who performed the action.
    /// </summary>
    public string? PersonIdKey { get; init; }

    /// <summary>
    /// Filter by specific entity that was affected.
    /// </summary>
    public string? EntityIdKey { get; init; }

    /// <summary>
    /// Page number for pagination (1-based).
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Number of results per page.
    /// </summary>
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Request parameters for exporting audit logs.
/// </summary>
public record AuditLogExportRequest
{
    /// <summary>
    /// Start of the date range to export (inclusive).
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// End of the date range to export (inclusive).
    /// </summary>
    public DateTime? EndDate { get; init; }

    /// <summary>
    /// Filter by entity type.
    /// </summary>
    public string? EntityType { get; init; }

    /// <summary>
    /// Filter by action type.
    /// </summary>
    public AuditAction? ActionType { get; init; }

    /// <summary>
    /// Filter by person who performed the action.
    /// </summary>
    public string? PersonIdKey { get; init; }

    /// <summary>
    /// Format for the exported data.
    /// </summary>
    public ExportFormat Format { get; init; } = ExportFormat.Csv;
}

/// <summary>
/// Supported export formats for audit log data.
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// Comma-separated values format.
    /// </summary>
    Csv = 0,

    /// <summary>
    /// JSON format.
    /// </summary>
    Json = 1,

    /// <summary>
    /// Excel spreadsheet format.
    /// </summary>
    Excel = 2
}
