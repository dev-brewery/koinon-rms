namespace Koinon.Application.DTOs;

/// <summary>
/// Request DTO for previewing a communication with merge fields replaced.
/// </summary>
public record CommunicationPreviewRequestDto
{
    /// <summary>
    /// Subject line (for email communications). Merge fields will be replaced.
    /// </summary>
    public string? Subject { get; init; }

    /// <summary>
    /// Message body. Merge fields will be replaced.
    /// </summary>
    public required string Body { get; init; }

    /// <summary>
    /// Optional IdKey of person to use for merge field data.
    /// If not provided, sample data will be used (John Doe).
    /// </summary>
    public string? PersonIdKey { get; init; }
}

/// <summary>
/// Response DTO for communication preview.
/// </summary>
public record CommunicationPreviewResponseDto
{
    /// <summary>
    /// Subject with merge fields replaced.
    /// </summary>
    public string? Subject { get; init; }

    /// <summary>
    /// Body with merge fields replaced.
    /// </summary>
    public required string Body { get; init; }

    /// <summary>
    /// Name of the person whose data was used for the preview.
    /// </summary>
    public required string PersonName { get; init; }
}
