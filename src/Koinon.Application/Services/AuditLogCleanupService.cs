using Koinon.Application.Interfaces;
using Koinon.Application.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Koinon.Application.Services;

/// <summary>
/// Service for cleaning up old audit log entries based on retention policy.
/// Called by Hangfire recurring job to periodically remove audit logs older than the configured retention period.
/// </summary>
public class AuditLogCleanupService(
    IApplicationDbContext context,
    IOptions<AuditRetentionSettings> settings,
    ILogger<AuditLogCleanupService> logger)
{
    private readonly IApplicationDbContext _context = context;
    private readonly AuditRetentionSettings _settings = settings.Value;
    private readonly ILogger<AuditLogCleanupService> _logger = logger;

    /// <summary>
    /// Deletes audit logs older than the configured retention period.
    /// Excludes entity types specified in the retention settings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Number of audit log records deleted.</returns>
    public async Task<int> CleanupAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Audit log cleanup is disabled. Skipping cleanup.");
            return 0;
        }

        var cutoffDate = DateTime.UtcNow.AddDays(-_settings.RetentionDays);
        _logger.LogInformation(
            "Starting audit log cleanup. Deleting records older than {CutoffDate} ({RetentionDays} days)",
            cutoffDate,
            _settings.RetentionDays);

        // Build query to find audit logs to delete
        var query = _context.AuditLogs
            .Where(log => log.Timestamp < cutoffDate);

        // Exclude specified entity types if configured
        if (_settings.ExcludedEntityTypes != null && _settings.ExcludedEntityTypes.Count > 0)
        {
            query = query.Where(log => !_settings.ExcludedEntityTypes.Contains(log.EntityType));
            _logger.LogDebug(
                "Excluding entity types: {ExcludedTypes}",
                string.Join(", ", _settings.ExcludedEntityTypes));
        }

        // Execute deletion
        var deletedCount = await query.ExecuteDeleteAsync(cancellationToken);

        _logger.LogInformation(
            "Audit log cleanup completed. Deleted {DeletedCount} records older than {CutoffDate}",
            deletedCount,
            cutoffDate);

        return deletedCount;
    }
}
