namespace Koinon.Application.DTOs;

/// <summary>
/// Full person details DTO.
/// </summary>
public class PersonDto
{
    public string IdKey { get; init; } = null!;
    public Guid Guid { get; init; }
    public string FirstName { get; init; } = null!;
    public string? NickName { get; init; }
    public string? MiddleName { get; init; }
    public string LastName { get; init; } = null!;
    public string FullName { get; init; } = null!;
    public DateOnly? BirthDate { get; init; }
    public int? Age { get; init; }
    public string Gender { get; init; } = null!;
    public string? Email { get; init; }
    public bool IsEmailActive { get; init; }
    public string EmailPreference { get; init; } = null!;
    public IReadOnlyList<PhoneNumberDto> PhoneNumbers { get; init; } = null!;
    public DefinedValueDto? RecordStatus { get; init; }
    public DefinedValueDto? ConnectionStatus { get; init; }
    public FamilySummaryDto? PrimaryFamily { get; init; }
    public CampusSummaryDto? PrimaryCampus { get; init; }
    public string? PhotoUrl { get; init; }
    public DateTime CreatedDateTime { get; init; }
    public DateTime? ModifiedDateTime { get; init; }
}

/// <summary>
/// Summary person DTO for lists and search results.
/// </summary>
public class PersonSummaryDto
{
    public string IdKey { get; init; } = null!;
    public string FirstName { get; init; } = null!;
    public string? NickName { get; init; }
    public string LastName { get; init; } = null!;
    public string FullName { get; init; } = null!;
    public string? Email { get; init; }
    public string? PhotoUrl { get; init; }
    public int? Age { get; init; }
    public string Gender { get; init; } = null!;
    public DefinedValueDto? ConnectionStatus { get; init; }
    public DefinedValueDto? RecordStatus { get; init; }
}

/// <summary>
/// Phone number DTO.
/// </summary>
public class PhoneNumberDto
{
    public string IdKey { get; init; } = null!;
    public string Number { get; init; } = null!;
    public string NumberFormatted { get; init; } = null!;
    public string? Extension { get; init; }
    public DefinedValueDto? PhoneType { get; init; }
    public bool IsMessagingEnabled { get; init; }
    public bool IsUnlisted { get; init; }
}

/// <summary>
/// Defined value DTO.
/// </summary>
public class DefinedValueDto
{
    public string IdKey { get; init; } = null!;
    public Guid Guid { get; init; }
    public string Value { get; init; } = null!;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public int Order { get; init; }
}

/// <summary>
/// Family summary DTO.
/// </summary>
public class FamilySummaryDto
{
    public string IdKey { get; init; } = null!;
    public string Name { get; init; } = null!;
    public int MemberCount { get; init; }
}

/// <summary>
/// Campus summary DTO.
/// </summary>
public class CampusSummaryDto
{
    public string IdKey { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? ShortCode { get; init; }
}
