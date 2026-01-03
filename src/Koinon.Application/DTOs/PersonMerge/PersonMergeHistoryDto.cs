namespace Koinon.Application.DTOs.PersonMerge;

/// <summary>
/// Audit record of a person merge operation.
/// </summary>
public class PersonMergeHistoryDto
{
    /// <summary>
    /// IdKey of the merge history record.
    /// </summary>
    public required string IdKey { get; set; }

    /// <summary>
    /// IdKey of the survivor person.
    /// </summary>
    public required string SurvivorIdKey { get; set; }

    /// <summary>
    /// Name of the survivor person.
    /// </summary>
    public required string SurvivorName { get; set; }

    /// <summary>
    /// IdKey of the merged person.
    /// </summary>
    public required string MergedIdKey { get; set; }

    /// <summary>
    /// Name of the merged person.
    /// </summary>
    public required string MergedName { get; set; }

    /// <summary>
    /// IdKey of the person who performed the merge.
    /// </summary>
    public string? MergedByIdKey { get; set; }

    /// <summary>
    /// Name of the person who performed the merge.
    /// </summary>
    public string? MergedByName { get; set; }

    /// <summary>
    /// Date and time when the merge was performed.
    /// </summary>
    public DateTime MergedDateTime { get; set; }

    /// <summary>
    /// Notes explaining the reason for the merge.
    /// </summary>
    public string? Notes { get; set; }
}
