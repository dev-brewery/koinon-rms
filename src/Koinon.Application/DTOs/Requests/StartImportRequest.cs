namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to start an import job.
/// </summary>
public record StartImportRequest
{
    public required Stream FileStream { get; init; }
    public required string FileName { get; init; }
    public required string ImportType { get; init; }
    public required Dictionary<string, string> FieldMappings { get; init; }
    public string? ImportTemplateIdKey { get; init; }
}
