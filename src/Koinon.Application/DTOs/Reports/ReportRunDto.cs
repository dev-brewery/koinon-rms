using Koinon.Domain.Enums;

namespace Koinon.Application.DTOs.Reports;

/// <summary>
/// Report run DTO representing a specific execution of a report.
/// </summary>
public record ReportRunDto
{
    public required string IdKey { get; init; }
    public required string ReportDefinitionIdKey { get; init; }
    public required string ReportName { get; init; }
    public required ReportStatus Status { get; init; }
    public string? Parameters { get; init; }
    public string? OutputFileIdKey { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? ErrorMessage { get; init; }
    public string? RequestedByName { get; init; }
    public DateTime CreatedDateTime { get; init; }
}

/// <summary>
/// Request to run a report with specific parameters.
/// </summary>
public record RunReportRequest
{
    public required string ReportDefinitionIdKey { get; init; }
    public string? Parameters { get; init; }
    public ReportOutputFormat? OutputFormat { get; init; }
}
