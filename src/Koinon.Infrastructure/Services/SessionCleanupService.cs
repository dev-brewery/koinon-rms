using Koinon.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Koinon.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of session cleanup service.
/// Removes old/expired supervisor sessions from the database to prevent unbounded growth.
/// </summary>
public class SessionCleanupService : ISessionCleanupService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly int _retentionDays;

    /// <summary>
    /// Initializes a new instance of the SessionCleanupService.
    /// </summary>
    /// <param name="context">Application database context.</param>
    /// <param name="configuration">Configuration for retention settings.</param>
    /// <param name="logger">Logger for tracking cleanup operations.</param>
    public SessionCleanupService(
        IApplicationDbContext context,
        IConfiguration configuration,
        ILogger<SessionCleanupService> logger)
    {
        _context = context;
        _logger = logger;
        _retentionDays = configuration.GetValue<int>("SessionCleanup:RetentionDays", 30);

        _logger.LogInformation("SessionCleanupService initialized with {RetentionDays} day retention", _retentionDays);
    }

    /// <inheritdoc />
    public async Task<int> CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-_retentionDays);

        _logger.LogInformation("Starting session cleanup. Deleting sessions older than {CutoffDate}", cutoffDate);

        try
        {
            // Find sessions to delete: either explicitly ended or expired beyond retention period
            var expiredSessions = await _context.SupervisorSessions
                .Where(s =>
                    // Session was ended AND it's old enough
                    (s.EndedAt != null && s.EndedAt < cutoffDate) ||
                    // Session expired AND it's old enough
                    (s.EndedAt == null && s.ExpiresAt < cutoffDate))
                .ToListAsync(cancellationToken);

            var deletedCount = expiredSessions.Count;

            if (deletedCount > 0)
            {
                _context.SupervisorSessions.RemoveRange(expiredSessions);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Session cleanup completed. Deleted {DeletedCount} sessions older than {CutoffDate}",
                    deletedCount,
                    cutoffDate);
            }
            else
            {
                _logger.LogInformation("Session cleanup completed. No sessions found older than {CutoffDate}", cutoffDate);
            }

            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during session cleanup for sessions older than {CutoffDate}", cutoffDate);
            throw;
        }
    }
}
