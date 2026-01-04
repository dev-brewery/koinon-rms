namespace Koinon.Domain.Enums;

/// <summary>
/// Represents the type of data being exported in an export job.
/// </summary>
public enum ExportType
{
    /// <summary>
    /// Export of person records and their associated data.
    /// </summary>
    People = 0,

    /// <summary>
    /// Export of family (group) records and relationships.
    /// </summary>
    Families = 1,

    /// <summary>
    /// Export of group records and memberships.
    /// </summary>
    Groups = 2,

    /// <summary>
    /// Export of financial contribution data.
    /// </summary>
    Contributions = 3,

    /// <summary>
    /// Export of attendance records.
    /// </summary>
    Attendance = 4,

    /// <summary>
    /// Custom export with user-defined entity type.
    /// </summary>
    Custom = 99
}
