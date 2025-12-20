namespace Koinon.Application.DTOs.Import;

/// <summary>
/// DTO for import job execution status and progress.
/// </summary>
public record ImportJobDto
{
    public required string IdKey { get; init; }
    public required Guid Guid { get; init; }
    public string? ImportTemplateIdKey { get; init; }
    public required string ImportType { get; init; }
    public required string Status { get; init; }
    public required string FileName { get; init; }
    public required int TotalRows { get; init; }
    public required int ProcessedRows { get; init; }
    public required int SuccessCount { get; init; }
    public required int ErrorCount { get; init; }
    public List<ImportRowError>? Errors { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public required DateTime CreatedDateTime { get; init; }
}
