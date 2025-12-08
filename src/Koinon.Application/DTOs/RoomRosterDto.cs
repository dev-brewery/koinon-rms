namespace Koinon.Application.DTOs;

/// <summary>
/// Represents a child on the room roster with all relevant information for teachers.
/// </summary>
public record RosterChildDto(
    string AttendanceIdKey,
    string PersonIdKey,
    string FullName,
    string FirstName,
    string LastName,
    string? NickName,
    string? PhotoUrl,
    int? Age,
    string? Grade,
    string? Allergies,
    bool HasCriticalAllergies,
    string? SpecialNeeds,
    string? SecurityCode,
    DateTime CheckInTime,
    string? ParentName,
    string? ParentMobilePhone,
    bool IsFirstTime);

/// <summary>
/// Represents the complete roster for a room/location.
/// </summary>
public record RoomRosterDto(
    string LocationIdKey,
    string LocationName,
    List<RosterChildDto> Children,
    int TotalCount,
    int? Capacity,
    DateTime GeneratedAt,
    bool IsAtCapacity,
    bool IsNearCapacity);

/// <summary>
/// Request for a printable roster.
/// </summary>
public record PrintableRosterRequestDto(
    string LocationIdKey,
    bool IncludePhotos = false,
    bool IncludeParentInfo = true,
    bool IncludeSpecialNeeds = true);
