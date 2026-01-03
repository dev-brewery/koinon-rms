namespace Koinon.Domain.Enums;

/// <summary>
/// Defines the type of masking to apply to sensitive data in audit logs.
/// </summary>
public enum SensitiveMaskType
{
    /// <summary>
    /// Replace the entire value with a mask (e.g., "***").
    /// </summary>
    Full = 0,

    /// <summary>
    /// Show only the last 4 characters, masking the rest (e.g., "****5678").
    /// </summary>
    Partial = 1,

    /// <summary>
    /// Show only the SHA256 hash of the value.
    /// </summary>
    Hash = 2
}
