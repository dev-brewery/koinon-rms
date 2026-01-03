namespace Koinon.Application.DTOs.PersonMerge;

/// <summary>
/// Request to mark two persons as "not duplicates" to exclude them from duplicate detection.
/// </summary>
public class IgnoreDuplicateRequestDto
{
    /// <summary>
    /// IdKey of the first person.
    /// </summary>
    public required string Person1IdKey { get; set; }

    /// <summary>
    /// IdKey of the second person.
    /// </summary>
    public required string Person2IdKey { get; set; }

    /// <summary>
    /// Optional reason for marking as not duplicates (e.g., "Father and son with same name").
    /// </summary>
    public string? Reason { get; set; }
}
