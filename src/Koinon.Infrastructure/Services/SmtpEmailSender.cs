using Koinon.Application.DTOs.Communications;
using Koinon.Application.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using Polly;
using Polly.Retry;

namespace Koinon.Infrastructure.Services;

/// <summary>
/// SMTP-based email sender implementation using MailKit.
/// </summary>
public class SmtpEmailSender(
    IConfiguration configuration,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    /// <summary>
    /// Creates a Polly v8 resilience pipeline for transient SMTP failures with exponential backoff.
    /// Retries on SocketException and SmtpProtocolException (transient).
    /// Does NOT retry on SmtpCommandException or AuthenticationException (permanent).
    /// </summary>
    private ResiliencePipeline CreateRetryPipeline()
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<System.Net.Sockets.SocketException>()
                    .Handle<SmtpProtocolException>(),
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(1),
                UseJitter = true,
                OnRetry = args =>
                {
                    logger.LogWarning(
                        args.Outcome.Exception,
                        "SMTP send attempt {RetryCount} failed. Retrying in {Delay}...",
                        args.AttemptNumber,
                        args.RetryDelay);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }
    public async Task<bool> SendEmailAsync(
        string toAddress,
        string? toName,
        string fromAddress,
        string? fromName,
        string subject,
        string bodyHtml,
        string? bodyText = null,
        IEnumerable<EmailAttachmentDto>? attachments = null,
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

            // Create body with HTML and optional plain text
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = bodyHtml
            };

            // Add plain text version if provided
            if (!string.IsNullOrWhiteSpace(bodyText))
            {
                bodyBuilder.TextBody = bodyText;
            }

            // Add attachments if provided
            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    bodyBuilder.Attachments.Add(
                        attachment.FileName,
                        attachment.Content,
                        ContentType.Parse(attachment.ContentType));
                }
            }

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

            // Send email with retry pipeline for transient failures
            var pipeline = CreateRetryPipeline();
            await pipeline.ExecuteAsync(async cancellationToken =>
            {
                using var client = new SmtpClient();
                try
                {
                    await client.ConnectAsync(smtpHost, smtpPort, smtpUseSsl, cancellationToken);

                    // Authenticate if credentials provided
                    if (!string.IsNullOrWhiteSpace(smtpUsername) && !string.IsNullOrWhiteSpace(smtpCredentials))
                    {
                        await client.AuthenticateAsync(smtpUsername, smtpCredentials, cancellationToken);
                    }

                    await client.SendAsync(message, cancellationToken);
                    await client.DisconnectAsync(true, cancellationToken);
                }
                catch (Exception) when (client.IsConnected)
                {
                    // Ensure graceful disconnect on failure before retry
                    await client.DisconnectAsync(false, cancellationToken);
                    throw;
                }
            }, ct);

            logger.LogInformation(
                "Email sent successfully to {ToAddress} with subject '{Subject}'",
                toAddress,
                subject);

            return true;
        }
        catch (SmtpCommandException ex)
        {
            // Permanent error - do not retry
            logger.LogError(ex, "SMTP command failed for {ToAddress} (permanent error)", toAddress);
            return false;
        }
        catch (SmtpProtocolException ex)
        {
            // Transient error - retries exhausted
            logger.LogError(ex, "SMTP protocol error for {ToAddress} after retries", toAddress);
            return false;
        }
        catch (System.Net.Sockets.SocketException ex)
        {
            // Transient error - retries exhausted
            logger.LogError(ex, "Network error sending to {ToAddress} after retries", toAddress);
            return false;
        }
        catch (MailKit.Security.AuthenticationException ex)
        {
            // Permanent error - do not retry
            logger.LogError(ex, "SMTP authentication failed (permanent error)");
            return false;
        }
    }
}
