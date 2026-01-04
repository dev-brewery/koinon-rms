using Koinon.Domain.Enums;

namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to update user preferences for theme, date format, and timezone.
/// </summary>
public record UpdateUserPreferenceRequest
{
    public required Theme Theme { get; init; }
    public required string DateFormat { get; init; }
    public required string TimeZone { get; init; }
}
