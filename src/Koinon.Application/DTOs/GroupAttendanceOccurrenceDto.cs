namespace Koinon.Application.DTOs;

/// <summary>
/// Summary of a single group attendance occurrence (one meeting date).
/// </summary>
public record GroupAttendanceOccurrenceDto(
    string IdKey,
    DateOnly OccurrenceDate,
    int AttendeeCount,
    int AnonymousCount,
    bool DidNotOccur,
    string? LocationName,
    string? ScheduleName);
