namespace Koinon.Domain.Enums;

/// <summary>
/// Represents the theme preference for user interface display.
/// </summary>
public enum Theme
{
    /// <summary>
    /// Use the system's theme setting (follows OS preference).
    /// </summary>
    System = 0,

    /// <summary>
    /// Light theme with dark text on light background.
    /// </summary>
    Light = 1,

    /// <summary>
    /// Dark theme with light text on dark background.
    /// </summary>
    Dark = 2
}
