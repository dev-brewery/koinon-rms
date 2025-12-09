namespace Koinon.Application.DTOs;

/// <summary>
/// Group membership request DTO.
/// </summary>
public record GroupMemberRequestDto
{
    public required string IdKey { get; init; }
    public required PersonSummaryDto Requester { get; init; }
    public required GroupSummaryDto Group { get; init; }
    public required string Status { get; init; }
    public string? RequestNote { get; init; }
    public string? ResponseNote { get; init; }
    public PersonSummaryDto? ProcessedByPerson { get; init; }
    public DateTime? ProcessedDateTime { get; init; }
    public required DateTime CreatedDateTime { get; init; }
    public DateTime? ModifiedDateTime { get; init; }
}
