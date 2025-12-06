namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to update an existing person.
/// </summary>
public record UpdatePersonRequest
{
    public string? FirstName { get; init; }
    public string? NickName { get; init; }
    public string? MiddleName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public bool? IsEmailActive { get; init; }
    public string? EmailPreference { get; init; }
    public string? Gender { get; init; }
    public DateOnly? BirthDate { get; init; }
    public string? ConnectionStatusValueId { get; init; }
    public string? RecordStatusValueId { get; init; }
    public string? PrimaryCampusId { get; init; }
}
