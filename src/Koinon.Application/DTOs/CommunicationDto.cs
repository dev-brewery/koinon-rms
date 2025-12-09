namespace Koinon.Application.DTOs;

/// <summary>
/// Full communication details DTO.
/// </summary>
public record CommunicationDto
{
    public required string IdKey { get; init; }
    public required Guid Guid { get; init; }
    public required string CommunicationType { get; init; }
    public required string Status { get; init; }
    public string? Subject { get; init; }
    public required string Body { get; init; }
    public string? FromEmail { get; init; }
    public string? FromName { get; init; }
    public string? ReplyToEmail { get; init; }
    public DateTime? SentDateTime { get; init; }
    public required int RecipientCount { get; init; }
    public required int DeliveredCount { get; init; }
    public required int FailedCount { get; init; }
    public required int OpenedCount { get; init; }
    public string? Note { get; init; }
    public required DateTime CreatedDateTime { get; init; }
    public DateTime? ModifiedDateTime { get; init; }
    public required IReadOnlyList<CommunicationRecipientDto> Recipients { get; init; }
}

/// <summary>
/// Summary communication DTO for lists.
/// </summary>
public record CommunicationSummaryDto
{
    public required string IdKey { get; init; }
    public required string CommunicationType { get; init; }
    public required string Status { get; init; }
    public string? Subject { get; init; }
    public required int RecipientCount { get; init; }
    public required int DeliveredCount { get; init; }
    public required int FailedCount { get; init; }
    public required DateTime CreatedDateTime { get; init; }
    public DateTime? SentDateTime { get; init; }
}

/// <summary>
/// Communication recipient DTO.
/// </summary>
public record CommunicationRecipientDto
{
    public required string IdKey { get; init; }
    public required string PersonIdKey { get; init; }
    public required string Address { get; init; }
    public string? RecipientName { get; init; }
    public required string Status { get; init; }
    public DateTime? DeliveredDateTime { get; init; }
    public DateTime? OpenedDateTime { get; init; }
    public string? ErrorMessage { get; init; }
    public string? GroupIdKey { get; init; }
}

/// <summary>
/// DTO for creating a new communication.
/// </summary>
public record CreateCommunicationDto
{
    public required string CommunicationType { get; init; }
    public string? Subject { get; init; }
    public required string Body { get; init; }
    public string? FromEmail { get; init; }
    public string? FromName { get; init; }
    public string? ReplyToEmail { get; init; }
    public string? Note { get; init; }
    public required IReadOnlyList<string> GroupIdKeys { get; init; }
}

/// <summary>
/// DTO for updating a communication.
/// </summary>
public record UpdateCommunicationDto
{
    public string? Subject { get; init; }
    public string? Body { get; init; }
    public string? FromEmail { get; init; }
    public string? FromName { get; init; }
    public string? ReplyToEmail { get; init; }
    public string? Note { get; init; }
}
