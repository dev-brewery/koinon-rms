using Koinon.Application.Interfaces;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Koinon.Infrastructure.Services;

/// <summary>
/// Background service that polls for pending communications and sends them.
/// Polls every 30 seconds and processes one communication at a time to respect rate limits.
/// </summary>
public class CommunicationSenderBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<CommunicationSenderBackgroundService> logger) : BackgroundService
{
    private const int PollingIntervalSeconds = 30;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Communication Sender Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingCommunicationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing pending communications");
            }

            // Wait before next poll
            await Task.Delay(TimeSpan.FromSeconds(PollingIntervalSeconds), stoppingToken);
        }

        logger.LogInformation("Communication Sender Background Service stopped");
    }

    /// <summary>
    /// Polls for pending communications and sends them one at a time.
    /// </summary>
    private async Task ProcessPendingCommunicationsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var communicationSender = scope.ServiceProvider.GetRequiredService<ICommunicationSender>();

        // Query for pending communications
        var pendingCommunications = await context.Communications
            .Where(c => c.Status == CommunicationStatus.Pending)
            .OrderBy(c => c.CreatedDateTime)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        if (pendingCommunications.Count == 0)
        {
            return; // No pending communications
        }

        logger.LogInformation(
            "Found {Count} pending communication(s) to process",
            pendingCommunications.Count);

        // Process one communication at a time to respect rate limits
        foreach (var communicationId in pendingCommunications)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await communicationSender.SendCommunicationAsync(communicationId, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to send communication {CommunicationId}",
                    communicationId);

                // Continue with next communication instead of stopping the whole process
            }
        }
    }
}
