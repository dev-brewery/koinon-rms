using Koinon.Application.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Koinon.Infrastructure.Services;

/// <summary>
/// SMTP-based email sender implementation using MailKit.
/// </summary>
public class SmtpEmailSender(
    IConfiguration configuration,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task<bool> SendEmailAsync(
        string toAddress,
        string? toName,
        string fromAddress,
        string? fromName,
        string subject,
        string bodyHtml,
        string? replyToAddress = null,
        CancellationToken ct = default)
    {
        try
        {
            var message = new MimeMessage();

            // Set from address
            message.From.Add(string.IsNullOrWhiteSpace(fromName)
                ? new MailboxAddress(fromAddress, fromAddress)
                : new MailboxAddress(fromName, fromAddress));

            // Set to address
            message.To.Add(string.IsNullOrWhiteSpace(toName)
                ? new MailboxAddress(toAddress, toAddress)
                : new MailboxAddress(toName, toAddress));

            // Set reply-to if provided
            if (!string.IsNullOrWhiteSpace(replyToAddress))
            {
                message.ReplyTo.Add(new MailboxAddress(replyToAddress, replyToAddress));
            }

            message.Subject = subject;

            // Create HTML body
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = bodyHtml
            };

            message.Body = bodyBuilder.ToMessageBody();

            // Get SMTP configuration with safe parsing
            var smtpHost = configuration["Smtp:Host"] ?? "localhost";

            if (!int.TryParse(configuration["Smtp:Port"], out var smtpPort))
            {
                smtpPort = 587;
                logger.LogWarning("Invalid SMTP port configuration, using default: 587");
            }

            var smtpUsername = configuration["Smtp:Username"];
            var smtpCredentials = configuration["Smtp:Password"];

            if (!bool.TryParse(configuration["Smtp:UseSsl"], out var smtpUseSsl))
            {
                smtpUseSsl = true;
                logger.LogWarning("Invalid SMTP UseSsl configuration, using default: true");
            }

            // Send email
            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, smtpUseSsl, ct);

            // Authenticate if credentials provided
            if (!string.IsNullOrWhiteSpace(smtpUsername) && !string.IsNullOrWhiteSpace(smtpCredentials))
            {
                await client.AuthenticateAsync(smtpUsername, smtpCredentials, ct);
            }

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            logger.LogInformation(
                "Email sent successfully to {ToAddress} with subject '{Subject}'",
                toAddress,
                subject);

            return true;
        }
        catch (MailKit.Net.Smtp.SmtpCommandException ex)
        {
            logger.LogError(ex, "SMTP command failed for {ToAddress}", toAddress);
            return false;
        }
        catch (MailKit.Net.Smtp.SmtpProtocolException ex)
        {
            logger.LogError(ex, "SMTP protocol error for {ToAddress}", toAddress);
            return false;
        }
        catch (System.Net.Sockets.SocketException ex)
        {
            logger.LogError(ex, "Network error sending to {ToAddress}", toAddress);
            return false;
        }
        catch (MailKit.Security.AuthenticationException ex)
        {
            logger.LogError(ex, "SMTP authentication failed");
            return false;
        }
    }
}
