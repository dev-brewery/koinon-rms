namespace Koinon.Domain.Enums;

/// <summary>
/// Type of data being imported.
/// </summary>
public enum ImportType
{
    /// <summary>
    /// People import (individuals and families).
    /// </summary>
    People = 1,

    /// <summary>
    /// Attendance records import.
    /// </summary>
    Attendance = 2,

    /// <summary>
    /// Giving/contribution records import.
    /// </summary>
    Giving = 3
}
