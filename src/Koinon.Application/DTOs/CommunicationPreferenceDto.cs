namespace Koinon.Application.DTOs;

/// <summary>
/// DTO representing a person's communication preference for a specific type.
/// </summary>
public record CommunicationPreferenceDto
{
    /// <summary>
    /// Encoded identifier for the preference.
    /// </summary>
    public required string IdKey { get; init; }

    /// <summary>
    /// Encoded identifier for the person who owns this preference.
    /// </summary>
    public required string PersonIdKey { get; init; }

    /// <summary>
    /// The type of communication (Email or Sms).
    /// </summary>
    public required string CommunicationType { get; init; }

    /// <summary>
    /// Whether the person has opted out of this communication type.
    /// </summary>
    public required bool IsOptedOut { get; init; }

    /// <summary>
    /// Date and time when the person opted out (null if not opted out).
    /// </summary>
    public DateTime? OptOutDateTime { get; init; }

    /// <summary>
    /// Optional reason provided when the person opted out (recommended when opting out).
    /// </summary>
    public string? OptOutReason { get; init; }
}

/// <summary>
/// DTO for updating a single communication preference.
/// </summary>
public record UpdateCommunicationPreferenceDto
{
    /// <summary>
    /// Whether the person should be opted out of this communication type.
    /// </summary>
    public required bool IsOptedOut { get; init; }

    /// <summary>
    /// Optional reason for opting out (required when IsOptedOut is true).
    /// </summary>
    public string? OptOutReason { get; init; }
}

/// <summary>
/// DTO for bulk updating multiple communication preferences.
/// </summary>
public record BulkUpdatePreferencesDto
{
    /// <summary>
    /// List of preferences to update.
    /// </summary>
    public required List<PreferenceUpdateItem> Preferences { get; init; }
}

/// <summary>
/// Individual preference update item for bulk operations.
/// </summary>
public record PreferenceUpdateItem
{
    /// <summary>
    /// The type of communication to update (Email or Sms).
    /// </summary>
    public required string CommunicationType { get; init; }

    /// <summary>
    /// Whether the person should be opted out of this communication type.
    /// </summary>
    public required bool IsOptedOut { get; init; }

    /// <summary>
    /// Optional reason for opting out.
    /// </summary>
    public string? OptOutReason { get; init; }
}
