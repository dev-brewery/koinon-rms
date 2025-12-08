using Koinon.Domain.Enums;

namespace Koinon.Application.DTOs;

/// <summary>
/// DTO representing a pager assignment for a checked-in child.
/// </summary>
public record PagerAssignmentDto(
    string IdKey,
    int PagerNumber,
    string AttendanceIdKey,
    string ChildName,
    string GroupName,
    string LocationName,
    string? ParentPhoneNumber,
    DateTime CheckedInAt,
    int MessagesSentCount
);

/// <summary>
/// DTO representing a message sent via the pager system.
/// </summary>
public record PagerMessageDto(
    string IdKey,
    PagerMessageType MessageType,
    string MessageText,
    PagerMessageStatus Status,
    DateTime SentDateTime,
    DateTime? DeliveredDateTime,
    string SentByPersonName
);

/// <summary>
/// DTO representing the full page history for a specific pager.
/// </summary>
public record PageHistoryDto(
    string IdKey,
    int PagerNumber,
    string ChildName,
    string ParentPhoneNumber,
    List<PagerMessageDto> Messages
);
