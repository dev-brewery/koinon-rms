using Koinon.Domain.Enums;

namespace Koinon.Application.DTOs.Exports;

/// <summary>
/// Export job DTO representing a specific execution of a data export.
/// </summary>
public record ExportJobDto
{
    public required string IdKey { get; init; }
    public required ExportType ExportType { get; init; }
    public string? EntityType { get; init; }
    public required ReportStatus Status { get; init; }
    public required ReportOutputFormat OutputFormat { get; init; }
    public required string Parameters { get; init; }
    public int? RecordCount { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? ErrorMessage { get; init; }
    public string? OutputFileIdKey { get; init; }
    public string? FileName { get; init; }
    public string? RequestedByPersonAliasIdKey { get; init; }
    public DateTime CreatedDateTime { get; init; }
}

/// <summary>
/// Request to start a new data export job.
/// </summary>
public record StartExportRequest
{
    public required ExportType ExportType { get; init; }
    public string? EntityType { get; init; }
    public required ReportOutputFormat OutputFormat { get; init; }
    public List<string>? Fields { get; init; }
    public Dictionary<string, string>? Filters { get; init; }
}
