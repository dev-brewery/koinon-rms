namespace Koinon.Application.DTOs;

/// <summary>
/// DTO for current user's profile information.
/// Extends PersonDto with self-service-specific fields.
/// </summary>
public record MyProfileDto
{
    public required string IdKey { get; init; }
    public required Guid Guid { get; init; }
    public required string FirstName { get; init; }
    public string? NickName { get; init; }
    public string? MiddleName { get; init; }
    public required string LastName { get; init; }
    public required string FullName { get; init; }
    public DateOnly? BirthDate { get; init; }
    public int? Age { get; init; }
    public required string Gender { get; init; }
    public string? Email { get; init; }
    public bool IsEmailActive { get; init; }
    public required string EmailPreference { get; init; }
    public required IReadOnlyList<PhoneNumberDto> PhoneNumbers { get; init; }
    public FamilySummaryDto? PrimaryFamily { get; init; }
    public CampusSummaryDto? PrimaryCampus { get; init; }
    public string? PhotoUrl { get; init; }
    public required DateTime CreatedDateTime { get; init; }
    public DateTime? ModifiedDateTime { get; init; }
}
