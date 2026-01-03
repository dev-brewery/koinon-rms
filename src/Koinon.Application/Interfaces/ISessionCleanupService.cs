namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for cleaning up old/expired sessions from the database.
/// Runs as a Hangfire recurring job to maintain database hygiene.
/// </summary>
public interface ISessionCleanupService
{
    /// <summary>
    /// Deletes sessions older than the configured retention period.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Count of deleted sessions.</returns>
    Task<int> CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default);
}
