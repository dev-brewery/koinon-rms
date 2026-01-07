namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to create a new person.
/// </summary>
public record CreatePersonRequest
{
    public required string FirstName { get; init; }
    public string? NickName { get; init; }
    public string? MiddleName { get; init; }
    public required string LastName { get; init; }
    public string? TitleValueId { get; init; }
    public string? SuffixValueId { get; init; }
    public string? Email { get; init; }
    public string? Gender { get; init; }
    public DateOnly? BirthDate { get; init; }
    public string? MaritalStatusValueId { get; init; }
    public DateOnly? AnniversaryDate { get; init; }
    public string? ConnectionStatusValueId { get; init; }
    public string? RecordStatusValueId { get; init; }
    public IList<CreatePhoneNumberRequest>? PhoneNumbers { get; init; }
    public string? FamilyId { get; init; }
    public string? FamilyRoleId { get; init; }
    public bool? CreateFamily { get; init; }
    public string? FamilyName { get; init; }
    public string? CampusId { get; init; }
}

/// <summary>
/// Request to create a phone number.
/// </summary>
public record CreatePhoneNumberRequest
{
    public required string Number { get; init; }
    public string? Extension { get; init; }
    public string? PhoneTypeValueId { get; init; }
    public bool? IsMessagingEnabled { get; init; }
}
