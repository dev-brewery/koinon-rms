using Koinon.Application.DTOs;

namespace Koinon.Application.DTOs.PersonMerge;

/// <summary>
/// Side-by-side comparison of two persons for merge decision.
/// </summary>
public class PersonComparisonDto
{
    /// <summary>
    /// Full details of the first person.
    /// </summary>
    public required PersonDto Person1 { get; set; }

    /// <summary>
    /// Full details of the second person.
    /// </summary>
    public required PersonDto Person2 { get; set; }

    /// <summary>
    /// Total attendance count for person 1.
    /// </summary>
    public int Person1AttendanceCount { get; set; }

    /// <summary>
    /// Total attendance count for person 2.
    /// </summary>
    public int Person2AttendanceCount { get; set; }

    /// <summary>
    /// Number of group memberships for person 1.
    /// </summary>
    public int Person1GroupMembershipCount { get; set; }

    /// <summary>
    /// Number of group memberships for person 2.
    /// </summary>
    public int Person2GroupMembershipCount { get; set; }

    /// <summary>
    /// Total contribution amount for person 1.
    /// </summary>
    public decimal Person1ContributionTotal { get; set; }

    /// <summary>
    /// Total contribution amount for person 2.
    /// </summary>
    public decimal Person2ContributionTotal { get; set; }
}
