using Koinon.Domain.Enums;

namespace Koinon.Application.DTOs;

/// <summary>
/// DTO representing the result of a pickup verification check.
/// </summary>
public record PickupVerificationResultDto(
    bool IsAuthorized,
    AuthorizationLevel? AuthorizationLevel,
    string? AuthorizedPickupIdKey,
    string Message,
    bool RequiresSupervisorOverride
);
