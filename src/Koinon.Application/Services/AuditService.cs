using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AutoMapper;
using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Koinon.Domain.Attributes;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for audit log operations.
/// Provides functionality for logging, querying, and exporting audit trail data.
/// </summary>
public class AuditService(
    IApplicationDbContext context,
    IUserContext userContext,
    IMapper mapper,
    ILogger<AuditService> logger) : IAuditService
{
    private readonly ILogger<AuditService> _logger = logger;
    /// <inheritdoc />
    public async Task LogAsync(
        AuditAction action,
        string entityType,
        string entityIdKey,
        string? oldValues,
        string? newValues,
        string? changedProperties,
        string? additionalInfo,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog
        {
            ActionType = action,
            EntityType = entityType,
            EntityIdKey = entityIdKey,
            PersonId = userContext.CurrentPersonId,
            Timestamp = DateTime.UtcNow,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ChangedProperties = changedProperties,
            AdditionalInfo = additionalInfo
        };

        context.AuditLogs.Add(auditLog);
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Audit log created: {Action} on {EntityType} {EntityIdKey} by Person {PersonId}",
            action,
            entityType,
            entityIdKey,
            userContext.CurrentPersonId);
    }

    /// <inheritdoc />
    public async Task<PagedResult<AuditLogDto>> SearchAsync(
        AuditLogSearchParameters parameters,
        CancellationToken cancellationToken = default)
    {
        IQueryable<AuditLog> query = context.AuditLogs
            .AsNoTracking()
            .Include(a => a.Person);

        // Apply filters
        if (parameters.StartDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= parameters.StartDate.Value);
        }

        if (parameters.EndDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= parameters.EndDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.EntityType))
        {
            query = query.Where(a => a.EntityType == parameters.EntityType);
        }

        if (parameters.ActionType.HasValue)
        {
            query = query.Where(a => a.ActionType == parameters.ActionType.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.PersonIdKey))
        {
            if (IdKeyHelper.TryDecode(parameters.PersonIdKey, out int personId))
            {
                query = query.Where(a => a.PersonId == personId);
            }
        }

        if (!string.IsNullOrWhiteSpace(parameters.EntityIdKey))
        {
            query = query.Where(a => a.EntityIdKey == parameters.EntityIdKey);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Get paginated results ordered by timestamp descending (most recent first)
        var auditLogs = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        var items = auditLogs.Select(a => mapper.Map<AuditLogDto>(a)).ToList();

        return new PagedResult<AuditLogDto>(
            items,
            totalCount,
            parameters.Page,
            parameters.PageSize);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditLogDto>> GetByEntityAsync(
        string entityType,
        string entityIdKey,
        CancellationToken cancellationToken = default)
    {
        var auditLogs = await context.AuditLogs
            .AsNoTracking()
            .Include(a => a.Person)
            .Where(a => a.EntityType == entityType && a.EntityIdKey == entityIdKey)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);

        return auditLogs.Select(a => mapper.Map<AuditLogDto>(a)).ToList();
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportAsync(
        AuditLogExportRequest request,
        CancellationToken cancellationToken = default)
    {
        IQueryable<AuditLog> query = context.AuditLogs
            .AsNoTracking()
            .Include(a => a.Person);

        // Apply filters (same logic as SearchAsync)
        if (request.StartDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= request.EndDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            query = query.Where(a => a.EntityType == request.EntityType);
        }

        if (request.ActionType.HasValue)
        {
            query = query.Where(a => a.ActionType == request.ActionType.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.PersonIdKey))
        {
            if (IdKeyHelper.TryDecode(request.PersonIdKey, out int personId))
            {
                query = query.Where(a => a.PersonId == personId);
            }
        }

        // Get all matching audit logs
        var auditLogs = await query
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);

        // Mask sensitive data
        var maskedLogs = auditLogs.Select(log => MaskSensitiveData(log)).ToList();

        // Generate export based on format
        return request.Format switch
        {
            ExportFormat.Csv => GenerateCsvExport(maskedLogs),
            ExportFormat.Json => GenerateJsonExport(maskedLogs),
            ExportFormat.Excel => throw new NotImplementedException("Excel export not yet implemented"),
            _ => throw new ArgumentException($"Unsupported export format: {request.Format}", nameof(request))
        };
    }

    /// <summary>
    /// Masks sensitive data in an audit log entry based on SensitiveDataAttribute.
    /// </summary>
    /// <param name="auditLog">The audit log to mask.</param>
    /// <returns>A new AuditLog with masked sensitive values.</returns>
    private AuditLog MaskSensitiveData(AuditLog auditLog)
    {
        // Get the entity type from the domain
        var entityType = GetEntityType(auditLog.EntityType);
        if (entityType is null)
        {
            return auditLog; // Can't mask if we don't know the entity type
        }

        return new AuditLog
        {
            Id = auditLog.Id,
            Guid = auditLog.Guid,
            ActionType = auditLog.ActionType,
            EntityType = auditLog.EntityType,
            EntityIdKey = auditLog.EntityIdKey,
            PersonId = auditLog.PersonId,
            Timestamp = auditLog.Timestamp,
            OldValues = MaskSensitiveJson(auditLog.OldValues, entityType),
            NewValues = MaskSensitiveJson(auditLog.NewValues, entityType),
            IpAddress = auditLog.IpAddress,
            UserAgent = auditLog.UserAgent,
            ChangedProperties = auditLog.ChangedProperties,
            AdditionalInfo = auditLog.AdditionalInfo,
            CreatedDateTime = auditLog.CreatedDateTime,
            ModifiedDateTime = auditLog.ModifiedDateTime,
            Person = auditLog.Person
        };
    }

    /// <summary>
    /// Masks sensitive properties in a JSON string based on entity type's SensitiveDataAttribute.
    /// </summary>
    /// <param name="json">JSON string to mask.</param>
    /// <param name="entityType">Type of the entity to check for sensitive properties.</param>
    /// <returns>Masked JSON string or original if null/empty.</returns>
    private string? MaskSensitiveJson(string? json, Type entityType)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return json;
        }

        try
        {
            var jsonDocument = JsonDocument.Parse(json);
            var rootElement = jsonDocument.RootElement;

            // Build dictionary of masked values
            var maskedValues = new Dictionary<string, object?>();

            foreach (var property in rootElement.EnumerateObject())
            {
                var propertyName = property.Name;
                var propertyValue = property.Value;

                // Check if this property has SensitiveDataAttribute
                var entityProperty = entityType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                var sensitiveAttr = entityProperty?.GetCustomAttribute<SensitiveDataAttribute>();

                if (sensitiveAttr != null && propertyValue.ValueKind == JsonValueKind.String)
                {
                    var originalValue = propertyValue.GetString();
                    maskedValues[propertyName] = MaskValue(originalValue, sensitiveAttr.MaskType);
                }
                else
                {
                    // Keep original value
                    maskedValues[propertyName] = GetJsonValue(propertyValue);
                }
            }

            return JsonSerializer.Serialize(maskedValues);
        }
        catch (JsonException ex)
        {
            // If we can't parse it, return original
            _logger.LogWarning(ex, "Failed to parse JSON for sensitive data masking");
            return json;
        }
    }

    /// <summary>
    /// Masks a sensitive value based on the mask type.
    /// </summary>
    /// <param name="value">Value to mask.</param>
    /// <param name="maskType">Type of masking to apply.</param>
    /// <returns>Masked value.</returns>
    private static string? MaskValue(string? value, SensitiveMaskType maskType)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
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
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Gets a .NET object from a JsonElement.
    /// </summary>
    /// <param name="element">JSON element to convert.</param>
    /// <returns>Object representation.</returns>
    private static object? GetJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out var i) ? i : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }

    /// <summary>
    /// Gets the entity Type from the domain based on entity type name.
    /// </summary>
    /// <param name="entityTypeName">Name of the entity type (e.g., "Person", "Group").</param>
    /// <returns>Type if found, null otherwise.</returns>
    private Type? GetEntityType(string entityTypeName)
    {
        // Get all types from the Domain assembly
        var domainAssembly = typeof(Entity).Assembly;
        return domainAssembly.GetTypes()
            .FirstOrDefault(t => t.Name.Equals(entityTypeName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Generates a CSV export of audit logs.
    /// </summary>
    /// <param name="auditLogs">Audit logs to export.</param>
    /// <returns>CSV file as byte array.</returns>
    private byte[] GenerateCsvExport(List<AuditLog> auditLogs)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("Timestamp,Action,Entity Type,Entity ID,Person,IP Address,User Agent,Changed Properties,Additional Info");

        // Rows
        foreach (var log in auditLogs)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsvValue(log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
                EscapeCsvValue(log.ActionType.ToString()),
                EscapeCsvValue(log.EntityType),
                EscapeCsvValue(log.EntityIdKey),
                EscapeCsvValue(log.Person?.FullName ?? "System"),
                EscapeCsvValue(log.IpAddress ?? ""),
                EscapeCsvValue(log.UserAgent ?? ""),
                EscapeCsvValue(log.ChangedProperties ?? ""),
                EscapeCsvValue(log.AdditionalInfo ?? "")));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    /// <summary>
    /// Escapes a value for CSV export.
    /// </summary>
    /// <param name="value">Value to escape.</param>
    /// <returns>Escaped value.</returns>
    private static string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        // If value contains comma, quote, or newline, wrap in quotes and escape internal quotes
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    /// <summary>
    /// Generates a JSON export of audit logs.
    /// </summary>
    /// <param name="auditLogs">Audit logs to export.</param>
    /// <returns>JSON file as byte array.</returns>
    private byte[] GenerateJsonExport(List<AuditLog> auditLogs)
    {
        var exportData = auditLogs.Select(log => new
        {
            log.Timestamp,
            Action = log.ActionType.ToString(),
            EntityType = log.EntityType,
            EntityIdKey = log.EntityIdKey,
            PersonName = log.Person?.FullName ?? "System",
            log.IpAddress,
            log.UserAgent,
            ChangedProperties = string.IsNullOrWhiteSpace(log.ChangedProperties)
                ? null
                : JsonSerializer.Deserialize<List<string>>(log.ChangedProperties),
            log.AdditionalInfo,
            OldValues = string.IsNullOrWhiteSpace(log.OldValues)
                ? null
                : JsonSerializer.Deserialize<object>(log.OldValues),
            NewValues = string.IsNullOrWhiteSpace(log.NewValues)
                ? null
                : JsonSerializer.Deserialize<object>(log.NewValues)
        }).ToList();

        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        return Encoding.UTF8.GetBytes(json);
    }
}
