namespace Koinon.Application.DTOs;

/// <summary>
/// Represents a merge field definition that can be used in templates.
/// Merge fields are replaced with person-specific data (e.g., {{FirstName}} → "John").
/// </summary>
/// <param name="Name">The field name without delimiters (e.g., "FirstName").</param>
/// <param name="Token">The full merge field token including delimiters (e.g., "{{FirstName}}").</param>
/// <param name="Description">Human-readable description of what this field represents.</param>
public record MergeFieldDto(
    string Name,
    string Token,
    string Description
);
