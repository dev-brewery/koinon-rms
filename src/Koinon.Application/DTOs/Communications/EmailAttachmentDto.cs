namespace Koinon.Application.DTOs.Communications;

/// <summary>
/// Represents an email attachment.
/// </summary>
public record EmailAttachmentDto(
    string FileName,
    byte[] Content,
    string ContentType);
