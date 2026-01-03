using Koinon.Application.DTOs.Communications;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Interface for sending email communications.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends an email asynchronously.
    /// </summary>
    /// <param name="toAddress">Recipient email address.</param>
    /// <param name="toName">Recipient name (optional).</param>
    /// <param name="fromAddress">Sender email address.</param>
    /// <param name="fromName">Sender name (optional).</param>
    /// <param name="subject">Email subject.</param>
    /// <param name="bodyHtml">HTML body content.</param>
    /// <param name="bodyText">Plain text body content (optional).</param>
    /// <param name="attachments">Email attachments (optional).</param>
    /// <param name="replyToAddress">Reply-to email address (optional).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if sent successfully, false otherwise.</returns>
    Task<bool> SendEmailAsync(
        string toAddress,
        string? toName,
        string fromAddress,
        string? fromName,
        string subject,
        string bodyHtml,
        string? bodyText = null,
        IEnumerable<EmailAttachmentDto>? attachments = null,
        string? replyToAddress = null,
        CancellationToken ct = default);
}
