namespace Koinon.Application.DTOs;

/// <summary>DTO for displaying a person note.</summary>
public record PersonNoteDto(
    string IdKey,
    string Text,
    DateTime NoteDate,
    string? NoteType,
    string? CreatedByName,
    bool IsPrivate,
    bool IsAlert,
    DateTime CreatedDateTime
);

/// <summary>Request to create a person note.</summary>
public record CreatePersonNoteRequest(
    string Text,
    DateTime NoteDate,
    string? NoteTypeDefinedValueIdKey,
    bool IsPrivate = false,
    bool IsAlert = false
);

/// <summary>Request to update a person note.</summary>
public record UpdatePersonNoteRequest(
    string Text,
    DateTime NoteDate,
    string? NoteTypeDefinedValueIdKey,
    bool IsPrivate = false,
    bool IsAlert = false
);
