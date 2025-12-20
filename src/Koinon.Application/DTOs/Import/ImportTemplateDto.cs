namespace Koinon.Application.DTOs.Import;

/// <summary>
/// DTO for import template with saved field mappings.
/// </summary>
public record ImportTemplateDto
{
    public required string IdKey { get; init; }
    public required Guid Guid { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string ImportType { get; init; }
    public required Dictionary<string, string> FieldMappings { get; init; }
    public required bool IsActive { get; init; }
    public required bool IsSystem { get; init; }
    public required DateTime CreatedDateTime { get; init; }
    public DateTime? ModifiedDateTime { get; init; }
}
