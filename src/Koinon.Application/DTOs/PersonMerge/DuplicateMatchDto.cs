namespace Koinon.Application.DTOs.PersonMerge;

/// <summary>
/// Represents a potential duplicate person match with scoring details.
/// </summary>
public class DuplicateMatchDto
{
    /// <summary>
    /// IdKey of the first person in the potential duplicate pair.
    /// </summary>
    public required string Person1IdKey { get; set; }

    /// <summary>
    /// Full name of the first person.
    /// </summary>
    public required string Person1Name { get; set; }

    /// <summary>
    /// Email address of the first person.
    /// </summary>
    public string? Person1Email { get; set; }

    /// <summary>
    /// Phone number of the first person.
    /// </summary>
    public string? Person1Phone { get; set; }

    /// <summary>
    /// Photo URL of the first person.
    /// </summary>
    public string? Person1PhotoUrl { get; set; }

    /// <summary>
    /// IdKey of the second person in the potential duplicate pair.
    /// </summary>
    public required string Person2IdKey { get; set; }

    /// <summary>
    /// Full name of the second person.
    /// </summary>
    public required string Person2Name { get; set; }

    /// <summary>
    /// Email address of the second person.
    /// </summary>
    public string? Person2Email { get; set; }

    /// <summary>
    /// Phone number of the second person.
    /// </summary>
    public string? Person2Phone { get; set; }

    /// <summary>
    /// Photo URL of the second person.
    /// </summary>
    public string? Person2PhotoUrl { get; set; }

    /// <summary>
    /// Match score as a percentage (0-100).
    /// </summary>
    public required int MatchScore { get; set; }

    /// <summary>
    /// List of reasons why these records are considered potential duplicates.
    /// </summary>
    public required List<string> MatchReasons { get; set; }
}
