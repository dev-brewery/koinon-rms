namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to create a new import template.
/// </summary>
public record CreateImportTemplateRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string ImportType { get; init; }
    public required Dictionary<string, string> FieldMappings { get; init; }
}
