using Koinon.Domain.Enums;

namespace Koinon.Domain.Entities;

/// <summary>
/// Audit log for financial data access and modifications.
/// Tracks all interactions with financial entities for compliance and security.
/// This is an append-only table designed for high-volume logging.
/// </summary>
public class FinancialAuditLog
{
    /// <summary>
    /// Primary key (BIGSERIAL in database).
    /// Uses long instead of int for high-volume audit logging.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Foreign key to the Person who performed the action.
    /// </summary>
    public int PersonId { get; set; }

    /// <summary>
    /// Type of action performed on the financial entity.
    /// </summary>
    public FinancialAuditAction ActionType { get; set; }

    /// <summary>
    /// Type of financial entity accessed.
    /// Examples: "Contribution", "Fund", "Batch", "ContributionDetail"
    /// </summary>
    public required string EntityType { get; set; }

    /// <summary>
    /// IdKey of the financial entity accessed.
    /// </summary>
    public required string EntityIdKey { get; set; }

    /// <summary>
    /// IP address of the client that initiated the action.
    /// Supports both IPv4 and IPv6 addresses.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string from the client (browser/application info).
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Additional context about the action in JSON format.
    /// Can include changed fields, old/new values, or other relevant details.
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// When the action occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    // Navigation properties

    /// <summary>
    /// The person who performed the audited action.
    /// </summary>
    public virtual Person Person { get; set; } = null!;
}
