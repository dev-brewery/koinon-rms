using Koinon.Domain.Enums;

namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to send a page to a parent.
/// </summary>
public record SendPageRequest(
    string PagerNumber,  // Can be "P-127" or just "127"
    PagerMessageType MessageType,
    string? CustomMessage  // Required if MessageType is Custom
);

/// <summary>
/// Request to search for pager assignments.
/// </summary>
public record PageSearchRequest(
    string? SearchTerm,  // Pager number or child name
    int? CampusId,
    DateTime? Date  // Defaults to today
);
