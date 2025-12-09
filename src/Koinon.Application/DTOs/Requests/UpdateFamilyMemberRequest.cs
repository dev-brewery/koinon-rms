namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request DTO for updating a family member's information.
/// Only allows updating limited fields for children by their parents.
/// </summary>
public record UpdateFamilyMemberRequest
{
    /// <summary>
    /// Preferred name or nickname.
    /// </summary>
    public string? NickName { get; init; }

    /// <summary>
    /// Phone numbers to update (replaces existing).
    /// Typically used for teen children's mobile phones.
    /// </summary>
    public IReadOnlyList<UpdatePhoneNumberRequest>? PhoneNumbers { get; init; }

    /// <summary>
    /// Known allergies.
    /// </summary>
    public string? Allergies { get; init; }

    /// <summary>
    /// Whether the child has critical allergies.
    /// </summary>
    public bool? HasCriticalAllergies { get; init; }

    /// <summary>
    /// Special needs or additional care notes.
    /// </summary>
    public string? SpecialNeeds { get; init; }
}
