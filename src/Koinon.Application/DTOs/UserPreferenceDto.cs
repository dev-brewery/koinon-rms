using Koinon.Domain.Enums;

namespace Koinon.Application.DTOs;

/// <summary>
/// DTO representing user display and localization preferences.
/// </summary>
public record UserPreferenceDto
{
    public required string IdKey { get; init; }
    public required Theme Theme { get; init; }
    public required string DateFormat { get; init; }
    public required string TimeZone { get; init; }
    public required DateTime CreatedDateTime { get; init; }
    public DateTime? ModifiedDateTime { get; init; }
}
