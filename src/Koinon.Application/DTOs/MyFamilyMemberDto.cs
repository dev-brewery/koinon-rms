namespace Koinon.Application.DTOs;

/// <summary>
/// DTO for family member information in self-service profile context.
/// Contains person details and edit permissions for the current user.
/// </summary>
public record MyFamilyMemberDto
{
    public required string IdKey { get; init; }
    public required string FirstName { get; init; }
    public string? NickName { get; init; }
    public required string LastName { get; init; }
    public required string FullName { get; init; }
    public DateOnly? BirthDate { get; init; }
    public int? Age { get; init; }
    public required string Gender { get; init; }
    public string? Email { get; init; }
    public required IReadOnlyList<PhoneNumberDto> PhoneNumbers { get; init; }
    public string? PhotoUrl { get; init; }
    public required string FamilyRole { get; init; }
    public bool CanEdit { get; init; }
    public string? Allergies { get; init; }
    public bool HasCriticalAllergies { get; init; }
    public string? SpecialNeeds { get; init; }
}
