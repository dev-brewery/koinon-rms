namespace Koinon.Application.DTOs;

/// <summary>
/// Full person details DTO.
/// </summary>
public record PersonDto
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
    public DefinedValueDto? RecordStatus { get; init; }
    public DefinedValueDto? ConnectionStatus { get; init; }
    public FamilySummaryDto? PrimaryFamily { get; init; }
    public CampusSummaryDto? PrimaryCampus { get; init; }
    public string? PhotoUrl { get; init; }
    public required DateTime CreatedDateTime { get; init; }
    public DateTime? ModifiedDateTime { get; init; }
}

/// <summary>
/// Summary person DTO for lists and search results.
/// </summary>
public record PersonSummaryDto
{
    public required string IdKey { get; init; }
    public required string FirstName { get; init; }
    public string? NickName { get; init; }
    public required string LastName { get; init; }
    public required string FullName { get; init; }
    public string? Email { get; init; }
    public string? PhotoUrl { get; init; }
    public int? Age { get; init; }
    public required string Gender { get; init; }
    public DefinedValueDto? ConnectionStatus { get; init; }
    public DefinedValueDto? RecordStatus { get; init; }
}

/// <summary>
/// Phone number DTO.
/// </summary>
public record PhoneNumberDto
{
    public required string IdKey { get; init; }
    public required string Number { get; init; }
    public required string NumberFormatted { get; init; }
    public string? Extension { get; init; }
    public DefinedValueDto? PhoneType { get; init; }
    public bool IsMessagingEnabled { get; init; }
    public bool IsUnlisted { get; init; }
}

/// <summary>
/// Defined value DTO.
/// </summary>
public record DefinedValueDto
{
    public required string IdKey { get; init; }
    public required Guid Guid { get; init; }
    public required string Value { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public int Order { get; init; }
}

/// <summary>
/// Family summary DTO.
/// </summary>
public record FamilySummaryDto
{
    public required string IdKey { get; init; }
    public required string Name { get; init; }
    public int MemberCount { get; init; }
}

/// <summary>
/// Campus summary DTO.
/// </summary>
public record CampusSummaryDto
{
    public required string IdKey { get; init; }
    public required string Name { get; init; }
    public string? ShortCode { get; init; }
}
