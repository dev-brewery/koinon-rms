namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to validate import mappings before execution.
/// </summary>
public record ValidateImportRequest
{
    public required Stream FileStream { get; init; }
    public required string FileName { get; init; }
    public required string ImportType { get; init; }
    public required Dictionary<string, string> FieldMappings { get; init; }
}
