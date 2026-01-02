namespace Koinon.Application.DTOs;

/// <summary>
/// Full communication template details DTO.
/// </summary>
public record CommunicationTemplateDto
{
    public required string IdKey { get; init; }
    public required Guid Guid { get; init; }
    public required string Name { get; init; }
    public required string CommunicationType { get; init; }
    public string? Subject { get; init; }
    public required string Body { get; init; }
    public string? Description { get; init; }
    public required bool IsActive { get; init; }
    public required DateTime CreatedDateTime { get; init; }
    public DateTime? ModifiedDateTime { get; init; }
}

/// <summary>
/// Summary communication template DTO for lists.
/// </summary>
public record CommunicationTemplateSummaryDto
{
    public required string IdKey { get; init; }
    public required string Name { get; init; }
    public required string CommunicationType { get; init; }
    public required bool IsActive { get; init; }
}

/// <summary>
/// DTO for creating a new communication template.
/// </summary>
public record CreateCommunicationTemplateDto
{
    public required string Name { get; init; }
    public required string CommunicationType { get; init; }
    public string? Subject { get; init; }
    public required string Body { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// DTO for updating a communication template.
/// </summary>
public record UpdateCommunicationTemplateDto
{
    public string? Name { get; init; }
    public string? CommunicationType { get; init; }
    public string? Subject { get; init; }
    public string? Body { get; init; }
    public string? Description { get; init; }
    public bool? IsActive { get; init; }
}
