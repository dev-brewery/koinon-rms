using Koinon.Domain.Enums;

namespace Koinon.Application.DTOs;

/// <summary>
/// DTO representing an authorized pickup person for a child.
/// </summary>
public record AuthorizedPickupDto(
    string IdKey,
    string ChildIdKey,
    string ChildName,
    string? AuthorizedPersonIdKey,
    string? AuthorizedPersonName,
    string? Name,
    string? PhoneNumber,
    PickupRelationship Relationship,
    AuthorizationLevel AuthorizationLevel,
    string? PhotoUrl,
    bool IsActive
);
