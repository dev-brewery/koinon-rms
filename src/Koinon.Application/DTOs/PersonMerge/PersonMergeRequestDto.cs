namespace Koinon.Application.DTOs.PersonMerge;

/// <summary>
/// Request to merge two person records.
/// </summary>
public class PersonMergeRequestDto
{
    /// <summary>
    /// IdKey of the person to keep (survivor).
    /// </summary>
    public required string SurvivorIdKey { get; set; }

    /// <summary>
    /// IdKey of the person to merge (will become inactive).
    /// </summary>
    public required string MergedIdKey { get; set; }

    /// <summary>
    /// Dictionary specifying which fields to keep from which person.
    /// Key: field name (e.g., "FirstName", "Email")
    /// Value: "survivor" or "merged" indicating which person's value to use
    /// </summary>
    public Dictionary<string, string>? FieldSelections { get; set; }

    /// <summary>
    /// Optional notes explaining the reason for the merge.
    /// </summary>
    public string? Notes { get; set; }
}
