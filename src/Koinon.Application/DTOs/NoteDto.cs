namespace Koinon.Application.DTOs;

/// <summary>
/// Read model for a pastoral note on a person's record.
/// </summary>
public record NoteDto
{
    public required string IdKey { get; init; }
    public required string NoteTypeValueIdKey { get; init; }
    public required string NoteTypeName { get; init; }
    public required string Text { get; init; }
    public required DateTime NoteDateTime { get; init; }
    public string? AuthorPersonIdKey { get; init; }
    public string? AuthorPersonName { get; init; }
    public bool IsPrivate { get; init; }
    public bool IsAlert { get; init; }
    public DateTime CreatedDateTime { get; init; }
    public DateTime? ModifiedDateTime { get; init; }
}

/// <summary>
/// Request to create a new note on a person's record.
/// </summary>
public record CreateNoteRequest
{
    public required string NoteTypeValueIdKey { get; init; }
    public required string Text { get; init; }

    /// <summary>
    /// When the interaction occurred. Defaults to now when null.
    /// </summary>
    public DateTime? NoteDateTime { get; init; }

    public bool IsPrivate { get; init; }
    public bool IsAlert { get; init; }
}

/// <summary>
/// Request to update fields on an existing note. All fields are optional.
/// </summary>
public record UpdateNoteRequest
{
    public string? NoteTypeValueIdKey { get; init; }
    public string? Text { get; init; }
    public DateTime? NoteDateTime { get; init; }
    public bool? IsPrivate { get; init; }
    public bool? IsAlert { get; init; }
}
