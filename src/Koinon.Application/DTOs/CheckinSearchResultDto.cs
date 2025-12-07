namespace Koinon.Application.DTOs;

/// <summary>
/// Family search result for check-in kiosk operations.
/// Includes family summary and all active members for quick selection.
/// </summary>
public record CheckinFamilySearchResultDto
{
    /// <summary>
    /// Family IdKey.
    /// </summary>
    public required string FamilyIdKey { get; init; }

    /// <summary>
    /// Family name.
    /// </summary>
    public required string FamilyName { get; init; }

    /// <summary>
    /// Family address summary (for display/verification).
    /// </summary>
    public string? AddressSummary { get; init; }

    /// <summary>
    /// Campus name (if assigned).
    /// </summary>
    public string? CampusName { get; init; }

    /// <summary>
    /// List of active family members available for check-in.
    /// </summary>
    public required IReadOnlyList<CheckinFamilyMemberDto> Members { get; init; }

    /// <summary>
    /// Number of recent check-ins for this family (optional, for context).
    /// </summary>
    public int RecentCheckInCount { get; init; }
}

/// <summary>
/// Family member summary for check-in selection.
/// Optimized for quick display and selection on kiosk touch screen.
/// </summary>
public record CheckinFamilyMemberDto
{
    /// <summary>
    /// Person IdKey.
    /// </summary>
    public required string PersonIdKey { get; init; }

    /// <summary>
    /// Person's full name (uses NickName if available).
    /// </summary>
    public required string FullName { get; init; }

    /// <summary>
    /// First name.
    /// </summary>
    public required string FirstName { get; init; }

    /// <summary>
    /// Last name.
    /// </summary>
    public required string LastName { get; init; }

    /// <summary>
    /// Nickname (if different from first name).
    /// </summary>
    public string? NickName { get; init; }

    /// <summary>
    /// Person's age (calculated from birth date).
    /// </summary>
    public int? Age { get; init; }

    /// <summary>
    /// Gender.
    /// </summary>
    public required string Gender { get; init; }

    /// <summary>
    /// Photo URL (or placeholder if no photo).
    /// </summary>
    public string? PhotoUrl { get; init; }

    /// <summary>
    /// Family role name (Adult, Child, etc.).
    /// </summary>
    public required string RoleName { get; init; }

    /// <summary>
    /// Is this person a child (used for security code requirements)?
    /// </summary>
    public bool IsChild { get; init; }

    /// <summary>
    /// Has this person checked in recently (within last 7 days)?
    /// </summary>
    public bool HasRecentCheckIn { get; init; }

    /// <summary>
    /// When this person last checked in (null if never).
    /// </summary>
    public DateTime? LastCheckIn { get; init; }

    /// <summary>
    /// Current school grade (calculated from graduation year).
    /// </summary>
    public string? Grade { get; init; }
}
