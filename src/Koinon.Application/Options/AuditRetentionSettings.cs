namespace Koinon.Application.Options;

/// <summary>
/// Configuration settings for audit log retention and cleanup.
/// Loaded from the "AuditRetention" configuration section.
/// </summary>
public record AuditRetentionSettings
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "AuditRetention";

    /// <summary>
    /// Number of days to retain audit logs before cleanup.
    /// Default: 365 days (1 year).
    /// </summary>
    public int RetentionDays { get; init; } = 365;

    /// <summary>
    /// List of entity types that should bypass retention and never be deleted.
    /// Example: ["Person", "Contribution", "FinancialTransaction"]
    /// Null or empty list means all entities are subject to retention policy.
    /// </summary>
    public List<string>? ExcludedEntityTypes { get; init; }

    /// <summary>
    /// Whether audit log cleanup is enabled.
    /// Default: true.
    /// Set to false to disable automatic cleanup (for compliance, legal hold, etc.).
    /// </summary>
    public bool Enabled { get; init; } = true;
}
