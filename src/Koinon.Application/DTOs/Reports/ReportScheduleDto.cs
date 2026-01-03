using Koinon.Domain.Enums;

namespace Koinon.Application.DTOs.Reports;

/// <summary>
/// Report schedule DTO for automated report generation.
/// </summary>
public record ReportScheduleDto
{
    public required string IdKey { get; init; }
    public required string ReportDefinitionIdKey { get; init; }
    public required string ReportName { get; init; }
    public required string CronExpression { get; init; }
    public required string TimeZone { get; init; }
    public string? Parameters { get; init; }
    public string? RecipientPersonAliasIds { get; init; }
    public required ReportOutputFormat OutputFormat { get; init; }
    public required bool IsActive { get; init; }
    public DateTime? LastRunAt { get; init; }
    public DateTime? NextRunAt { get; init; }
}

/// <summary>
/// Request to create a new report schedule.
/// </summary>
public record CreateReportScheduleRequest
{
    public required string ReportDefinitionIdKey { get; init; }
    public required string CronExpression { get; init; }
    public string TimeZone { get; init; } = "America/New_York";
    public string? Parameters { get; init; }
    public string? RecipientPersonAliasIds { get; init; }
    public ReportOutputFormat OutputFormat { get; init; } = ReportOutputFormat.Pdf;
}

/// <summary>
/// Request to update an existing report schedule.
/// </summary>
public record UpdateReportScheduleRequest
{
    public string? CronExpression { get; init; }
    public string? TimeZone { get; init; }
    public string? Parameters { get; init; }
    public string? RecipientPersonAliasIds { get; init; }
    public ReportOutputFormat? OutputFormat { get; init; }
    public bool? IsActive { get; init; }
}
