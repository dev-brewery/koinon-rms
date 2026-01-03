using Koinon.Domain.Enums;

namespace Koinon.Application.DTOs.Reports;

/// <summary>
/// Report definition DTO containing metadata and configuration.
/// </summary>
public record ReportDefinitionDto
{
    public required string IdKey { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required ReportType ReportType { get; init; }
    public string? ParameterSchema { get; init; }
    public string? DefaultParameters { get; init; }
    public required ReportOutputFormat OutputFormat { get; init; }
    public required bool IsActive { get; init; }
    public required bool IsSystem { get; init; }
    public DateTime CreatedDateTime { get; init; }
    public DateTime? ModifiedDateTime { get; init; }
}

/// <summary>
/// Request to create a new report definition.
/// </summary>
public record CreateReportDefinitionRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required ReportType ReportType { get; init; }
    public string? ParameterSchema { get; init; }
    public string? DefaultParameters { get; init; }
    public ReportOutputFormat OutputFormat { get; init; } = ReportOutputFormat.Pdf;
}

/// <summary>
/// Request to update an existing report definition.
/// </summary>
public record UpdateReportDefinitionRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? ParameterSchema { get; init; }
    public string? DefaultParameters { get; init; }
    public ReportOutputFormat? OutputFormat { get; init; }
    public bool? IsActive { get; init; }
}
