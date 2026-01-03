using System.Reflection;
using System.Text.Json;
using Koinon.Application.Interfaces;
using Koinon.Domain.Attributes;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Koinon.Infrastructure.Interceptors;

/// <summary>
/// EF Core interceptor that automatically captures entity changes and creates audit log entries.
/// Hooks into SaveChangesAsync to inspect tracked entities before/after save operations.
/// </summary>
public class AuditSaveChangesInterceptor(
    IUserContext userContext,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuditSaveChangesInterceptor> logger) : SaveChangesInterceptor
{
    private readonly List<(AuditLog AuditLog, Entity TrackedEntity)> _pendingAudits = [];
    private bool _isUpdatingPendingAudits = false;
    /// <inheritdoc />
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null || _isUpdatingPendingAudits)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var context = eventData.Context;
        var auditLogs = new List<AuditLog>();

        // Get IP address and User Agent from HTTP context
        var ipAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();

        // Get all tracked entities with changes
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added ||
                       e.State == EntityState.Modified ||
                       e.State == EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            // Skip entities marked with [NoAudit] attribute
            if (entry.Entity.GetType().GetCustomAttribute<NoAuditAttribute>() != null)
            {
                continue;
            }

            // Skip AuditLog itself to prevent recursion
            if (entry.Entity is AuditLog)
            {
                continue;
            }

            // Skip entities that don't inherit from Entity base class
            if (entry.Entity is not Entity entityBase)
            {
                continue;
            }

            try
            {
                var auditLog = CreateAuditLog(entry, entityBase, ipAddress, userAgent);
                if (auditLog != null)
                {
                    auditLogs.Add(auditLog);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to create audit log for {EntityType} {EntityId}",
                    entry.Entity.GetType().Name,
                    entityBase.Id);
            }
        }

        // Add audit log entries to context before actual save
        if (auditLogs.Count > 0)
        {
            await context.AddRangeAsync(auditLogs, cancellationToken);

            // Track pending audits (those with "pending" IdKey) for post-save update
            foreach (var auditLog in auditLogs.Where(a => a.EntityIdKey == "pending"))
            {
                // Find the tracked entity this audit log refers to
                var trackedEntity = entries
                    .Where(e => e.Entity is Entity entity && entity.GetType().Name == auditLog.EntityType)
                    .Select(e => e.Entity as Entity)
                    .FirstOrDefault(e => e != null && e.Id == 0); // New entities have Id == 0

                if (trackedEntity != null)
                {
                    _pendingAudits.Add((auditLog, trackedEntity));
                }
            }
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        // Update pending audit logs with actual IdKeys after save
        // Use flag to prevent infinite recursion when we save again
        if (_pendingAudits.Count > 0 && eventData.Context != null && !_isUpdatingPendingAudits)
        {
            var context = eventData.Context;

            foreach (var (auditLog, trackedEntity) in _pendingAudits)
            {
                // Entity now has its generated Id, update the audit log's EntityIdKey
                if (trackedEntity.Id > 0)
                {
                    auditLog.EntityIdKey = trackedEntity.IdKey;
                }
            }

            // Clear pending audits and set flag before nested save
            _pendingAudits.Clear();
            _isUpdatingPendingAudits = true;

            try
            {
                // Save the updated audit logs (this triggers a nested save)
                await context.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                _isUpdatingPendingAudits = false;
            }
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Creates an audit log entry for a tracked entity change.
    /// </summary>
    /// <param name="entry">EF Core change tracker entry.</param>
    /// <param name="entityBase">Entity base instance for extracting IdKey.</param>
    /// <param name="ipAddress">IP address of the request.</param>
    /// <param name="userAgent">User agent of the request.</param>
    /// <returns>AuditLog entry or null if audit should be skipped.</returns>
    private AuditLog? CreateAuditLog(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry,
        Entity entityBase,
        string? ipAddress,
        string? userAgent)
    {
        var entityType = entry.Entity.GetType();
        var entityTypeName = entityType.Name;

        // Determine action type based on entity state
        var actionType = entry.State switch
        {
            EntityState.Added => AuditAction.Create,
            EntityState.Modified => AuditAction.Update,
            EntityState.Deleted => AuditAction.Delete,
            _ => AuditAction.Other
        };

        // For Added state, entity might not have Id yet (identity insert)
        // We'll use a placeholder and update it after save in a post-save hook if needed
        var entityIdKey = entityBase.Id > 0 ? entityBase.IdKey : "pending";

        // Capture old values (for Modified/Deleted)
        string? oldValues = null;
        if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
        {
            oldValues = SerializeEntityValues(entry, entityType, useOriginalValues: true);
        }

        // Capture new values (for Added/Modified)
        string? newValues = null;
        if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
        {
            newValues = SerializeEntityValues(entry, entityType, useOriginalValues: false);
        }

        // Get changed property names (for Modified)
        string? changedProperties = null;
        if (entry.State == EntityState.Modified)
        {
            var changes = entry.Properties
                .Where(p => p.IsModified)
                .Select(p => p.Metadata.Name)
                .ToList();

            if (changes.Count > 0)
            {
                changedProperties = JsonSerializer.Serialize(changes);
            }
        }

        return new AuditLog
        {
            ActionType = actionType,
            EntityType = entityTypeName,
            EntityIdKey = entityIdKey,
            PersonId = userContext.CurrentPersonId,
            Timestamp = DateTime.UtcNow,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ChangedProperties = changedProperties,
            AdditionalInfo = null
        };
    }

    /// <summary>
    /// Serializes entity property values to JSON, masking sensitive data.
    /// </summary>
    /// <param name="entry">EF Core change tracker entry.</param>
    /// <param name="entityType">Type of the entity.</param>
    /// <param name="useOriginalValues">If true, use OriginalValues; otherwise use CurrentValues.</param>
    /// <returns>JSON string of entity values.</returns>
    private string? SerializeEntityValues(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry,
        Type entityType,
        bool useOriginalValues)
    {
        var values = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            var propertyName = property.Metadata.Name;
            var propertyValue = useOriginalValues
                ? property.OriginalValue
                : property.CurrentValue;

            // Check if property has SensitiveDataAttribute
            var entityProperty = entityType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            var sensitiveAttr = entityProperty?.GetCustomAttribute<SensitiveDataAttribute>();

            if (sensitiveAttr != null && propertyValue is string stringValue)
            {
                // Mask sensitive data
                propertyValue = MaskValue(stringValue, sensitiveAttr.MaskType);
            }

            values[propertyName] = propertyValue;
        }

        try
        {
            return JsonSerializer.Serialize(values);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to serialize entity values for {EntityType}", entityType.Name);
            return null;
        }
    }

    /// <summary>
    /// Masks a sensitive value based on the mask type.
    /// </summary>
    /// <param name="value">Value to mask.</param>
    /// <param name="maskType">Type of masking to apply.</param>
    /// <returns>Masked value.</returns>
    private static string MaskValue(string? value, SensitiveMaskType maskType)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return maskType switch
        {
            SensitiveMaskType.Full => "***",
            SensitiveMaskType.Partial => value.Length > 4
                ? new string('*', value.Length - 4) + value[^4..]
                : "***",
            SensitiveMaskType.Hash => ComputeSha256Hash(value),
            _ => value
        };
    }

    /// <summary>
    /// Computes SHA256 hash of a string.
    /// </summary>
    /// <param name="value">Value to hash.</param>
    /// <returns>Hex-encoded hash.</returns>
    private static string ComputeSha256Hash(string value)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
