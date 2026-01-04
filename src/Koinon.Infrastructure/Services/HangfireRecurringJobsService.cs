using Hangfire;
using Koinon.Application.Interfaces;
using Microsoft.Extensions.Hosting;

namespace Koinon.Infrastructure.Services;

/// <summary>
/// Hosted service that registers Hangfire recurring jobs after application startup.
/// Ensures recurring jobs are registered after Hangfire storage is fully initialized.
/// </summary>
public class HangfireRecurringJobsService : IHostedService
{
    private readonly IRecurringJobManager _recurringJobManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="HangfireRecurringJobsService"/> class.
    /// </summary>
    /// <param name="recurringJobManager">Hangfire recurring job manager from DI.</param>
    public HangfireRecurringJobsService(IRecurringJobManager recurringJobManager)
    {
        _recurringJobManager = recurringJobManager;
    }

    /// <summary>
    /// Registers recurring jobs when the application starts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A completed task.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Register session cleanup recurring job
        // Runs daily at 2 AM UTC
        _recurringJobManager.AddOrUpdate<ISessionCleanupService>(
            "session-cleanup",
            service => service.CleanupExpiredSessionsAsync(CancellationToken.None),
            "0 2 * * *", // Daily at 2 AM UTC
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        // Register scheduled communication processor recurring job
        // Runs every minute to check for scheduled communications that need to be sent
        _recurringJobManager.AddOrUpdate<IScheduledCommunicationProcessor>(
            "scheduled-communication-processor",
            processor => processor.ProcessScheduledCommunicationsAsync(CancellationToken.None),
            "* * * * *", // Every minute
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        return Task.CompletedTask;
    }

    /// <summary>
    /// No action needed when stopping.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A completed task.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
