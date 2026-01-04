using Koinon.Domain.Enums;

namespace Koinon.Domain.Entities;

/// <summary>
/// Stores user-specific preferences for display and localization settings.
/// Each person can have their own theme, date format, and timezone preferences.
/// </summary>
public class UserPreference : Entity
{
    /// <summary>
    /// Foreign key to the Person who owns these preferences.
    /// </summary>
    public required int PersonId { get; set; }

    /// <summary>
    /// The user's preferred theme (System, Light, or Dark).
    /// Defaults to System which follows the operating system preference.
    /// </summary>
    public Theme Theme { get; set; } = Theme.System;

    /// <summary>
    /// The user's preferred date format string (e.g., "MM/dd/yyyy", "dd/MM/yyyy").
    /// Maximum length: 20 characters.
    /// </summary>
    public required string DateFormat { get; set; }

    /// <summary>
    /// IANA timezone identifier for the user's timezone (e.g., "America/New_York").
    /// Maximum length: 64 characters.
    /// </summary>
    public required string TimeZone { get; set; }

    /// <summary>
    /// Navigation property to the Person who owns these preferences.
    /// </summary>
    public virtual Person? Person { get; set; }
}
