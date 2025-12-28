namespace Koinon.Application.DTOs;

/// <summary>
/// Person DTO for testing graph generation.
/// Follows project conventions: uses IdKey (NOT int Id), record type.
/// </summary>
public record PersonDto
{
    public required string IdKey { get; init; }
    public required Guid Guid { get; init; }
    public required string FirstName { get; init; }
    public string? NickName { get; init; }
    public required string LastName { get; init; }
    public required string FullName { get; init; }
    public string? Email { get; init; }
    public bool IsEmailActive { get; init; }
    public DateOnly? BirthDate { get; init; }
    public int? Age { get; init; }
    public required string Gender { get; init; }
    public required IReadOnlyList<PhoneNumberDto> PhoneNumbers { get; init; }
    public required DateTime CreatedDateTime { get; init; }
    public DateTime? ModifiedDateTime { get; init; }
}

/// <summary>
/// Person summary DTO for lists.
/// </summary>
public record PersonSummaryDto
{
    public required string IdKey { get; init; }
    public required string FirstName { get; init; }
    public string? NickName { get; init; }
    public required string LastName { get; init; }
    public required string FullName { get; init; }
    public string? Email { get; init; }
    public int? Age { get; init; }
}

/// <summary>
/// Phone number DTO.
/// </summary>
public record PhoneNumberDto
{
    public required string IdKey { get; init; }
    public required string Number { get; init; }
    public required string NumberFormatted { get; init; }
}
