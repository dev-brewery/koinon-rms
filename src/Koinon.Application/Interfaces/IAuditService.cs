using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Domain.Enums;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service interface for audit log operations.
/// Provides functionality for logging, querying, and exporting audit trail data.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Creates a new audit log entry for an action performed on an entity.
    /// </summary>
    /// <param name="action">Type of action performed (Create, Update, Delete, etc.)</param>
    /// <param name="entityType">Name of the entity type (e.g., "Person", "Group")</param>
    /// <param name="entityIdKey">IdKey of the entity being audited</param>
    /// <param name="oldValues">JSON representation of entity values before the change (null for Create actions)</param>
    /// <param name="newValues">JSON representation of entity values after the change (null for Delete actions)</param>
    /// <param name="changedProperties">JSON array of property names that changed (for Update actions)</param>
    /// <param name="additionalInfo">Optional contextual information about the action</param>
    /// <param name="ipAddress">Optional IP address of the client making the request</param>
    /// <param name="userAgent">Optional User-Agent header from the request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task LogAsync(
        AuditAction action,
        string entityType,
        string entityIdKey,
        string? oldValues,
        string? newValues,
        string? changedProperties,
        string? additionalInfo,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for audit log entries with pagination and filtering.
    /// </summary>
    /// <param name="parameters">Search and filter parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated result of audit log entries</returns>
    Task<PagedResult<AuditLogDto>> SearchAsync(
        AuditLogSearchParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all audit log entries for a specific entity.
    /// Returns entries in chronological order (oldest to newest).
    /// </summary>
    /// <param name="entityType">Name of the entity type (e.g., "Person", "Group")</param>
    /// <param name="entityIdKey">IdKey of the entity</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of audit log entries for the entity</returns>
    Task<IEnumerable<AuditLogDto>> GetByEntityAsync(
        string entityType,
        string entityIdKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports audit log entries to a file based on the specified request parameters.
    /// Supports various export formats (CSV, Excel, etc.) and filtering options.
    /// </summary>
    /// <param name="request">Export request containing filters and format preferences</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Byte array containing the exported file data</returns>
    Task<byte[]> ExportAsync(
        AuditLogExportRequest request,
        CancellationToken cancellationToken = default);
}
