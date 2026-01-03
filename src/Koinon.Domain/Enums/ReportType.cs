namespace Koinon.Domain.Enums;

/// <summary>
/// Represents the type of report being generated.
/// </summary>
public enum ReportType
{
    /// <summary>
    /// Attendance summary report showing check-in statistics and trends.
    /// </summary>
    AttendanceSummary = 1,

    /// <summary>
    /// Giving summary report showing financial contributions and trends.
    /// </summary>
    GivingSummary = 2,

    /// <summary>
    /// Directory report containing contact information for people or groups.
    /// </summary>
    Directory = 3,

    /// <summary>
    /// Custom user-defined report with specific criteria.
    /// </summary>
    Custom = 99
}
