namespace Koinon.Application.DTOs.PersonMerge;

/// <summary>
/// Result of a person merge operation with details about what was updated.
/// </summary>
public class PersonMergeResultDto
{
    /// <summary>
    /// IdKey of the survivor person.
    /// </summary>
    public required string SurvivorIdKey { get; set; }

    /// <summary>
    /// IdKey of the merged person (now inactive).
    /// </summary>
    public required string MergedIdKey { get; set; }

    /// <summary>
    /// Number of PersonAlias records updated.
    /// </summary>
    public int AliasesUpdated { get; set; }

    /// <summary>
    /// Number of GroupMember records updated or merged.
    /// </summary>
    public int GroupMembershipsUpdated { get; set; }

    /// <summary>
    /// Number of FamilyMember records updated.
    /// </summary>
    public int FamilyMembershipsUpdated { get; set; }

    /// <summary>
    /// Number of PhoneNumber records updated.
    /// </summary>
    public int PhoneNumbersUpdated { get; set; }

    /// <summary>
    /// Number of AuthorizedPickup records updated.
    /// </summary>
    public int AuthorizedPickupsUpdated { get; set; }

    /// <summary>
    /// Number of CommunicationPreference records updated.
    /// </summary>
    public int CommunicationPreferencesUpdated { get; set; }

    /// <summary>
    /// Number of RefreshToken records updated.
    /// </summary>
    public int RefreshTokensUpdated { get; set; }

    /// <summary>
    /// Number of PersonSecurityRole records updated.
    /// </summary>
    public int SecurityRolesUpdated { get; set; }

    /// <summary>
    /// Number of SupervisorSession records updated.
    /// </summary>
    public int SupervisorSessionsUpdated { get; set; }

    /// <summary>
    /// Number of FollowUp records updated.
    /// </summary>
    public int FollowUpsUpdated { get; set; }

    /// <summary>
    /// Total number of records updated across all tables.
    /// </summary>
    public int TotalRecordsUpdated { get; set; }

    /// <summary>
    /// Date and time when the merge was completed.
    /// </summary>
    public DateTime MergedDateTime { get; set; }
}
