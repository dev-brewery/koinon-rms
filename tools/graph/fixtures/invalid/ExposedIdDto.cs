namespace Koinon.Application.DTOs;

/// <summary>
/// INVALID: DTO that exposes integer ID instead of IdKey.
/// This violates Rule 04: API Design and should be detected.
/// </summary>
public record ExposedIdDto
{
    /// <summary>
    /// VIOLATION: Exposing integer ID directly.
    /// Should use IdKey instead.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Guid property.
    /// </summary>
    public Guid Guid { get; init; }

    /// <summary>
    /// Name property.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Email property.
    /// </summary>
    public string? Email { get; init; }
}

/// <summary>
/// INVALID: Another DTO with exposed integer ID.
/// </summary>
public record BadPersonDto
{
    public required int Id { get; init; }  // VIOLATION
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
}
