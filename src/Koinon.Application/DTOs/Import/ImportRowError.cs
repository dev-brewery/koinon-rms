namespace Koinon.Application.DTOs.Import;

/// <summary>
/// Represents a single row-level error during import.
/// </summary>
public record ImportRowError
{
    public required int Row { get; init; }
    public required string Column { get; init; }
    public required string Value { get; init; }
    public required string Message { get; init; }
}
