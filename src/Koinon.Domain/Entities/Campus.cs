namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a physical church location/site in a multi-site church context.
/// </summary>
public class Campus : Entity
{
    /// <summary>
    /// Name of the campus (e.g., "Main Campus", "West Campus").
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Abbreviated name for the campus (e.g., "MAIN", "WEST").
    /// Maximum 10 characters.
    /// </summary>
    public string? ShortCode { get; set; }

    /// <summary>
    /// Detailed description of the campus.
    /// Maximum 500 characters.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Indicates whether this campus is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Campus-specific website URL.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Main phone number for the campus.
    /// Maximum 20 characters.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// IANA time zone identifier (e.g., "America/New_York", "America/Los_Angeles").
    /// Maximum 50 characters.
    /// </summary>
    public string? TimeZoneId { get; set; }

    /// <summary>
    /// Foreign key to DefinedValue indicating the campus status.
    /// </summary>
    public int? CampusStatusValueId { get; set; }

    /// <summary>
    /// Foreign key to PersonAlias for the campus leader.
    /// </summary>
    public int? LeaderPersonAliasId { get; set; }

    /// <summary>
    /// Service times for this campus (JSON or semicolon-separated format).
    /// </summary>
    public string? ServiceTimes { get; set; }

    /// <summary>
    /// Display order for sorting campuses.
    /// </summary>
    public int Order { get; set; }

    // Navigation properties

    /// <summary>
    /// Navigation property to the campus status DefinedValue.
    /// </summary>
    public virtual DefinedValue? CampusStatusValue { get; set; }

    /// <summary>
    /// Collection of groups associated with this campus.
    /// </summary>
    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();
}
