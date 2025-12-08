namespace Koinon.Domain.Enums;

/// <summary>
/// Time ranges for filtering group meeting times.
/// </summary>
public enum TimeRange
{
    /// <summary>
    /// Morning hours: 6:00 AM - 12:00 PM
    /// </summary>
    Morning = 0,

    /// <summary>
    /// Afternoon hours: 12:00 PM - 5:00 PM
    /// </summary>
    Afternoon = 1,

    /// <summary>
    /// Evening hours: 5:00 PM - 10:00 PM
    /// </summary>
    Evening = 2
}
