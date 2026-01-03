using Koinon.Domain.Enums;

namespace Koinon.Domain.Attributes;

/// <summary>
/// Marks a property as containing sensitive data that should be masked in audit logs.
/// The masking behavior is determined by the <see cref="MaskType"/> property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class SensitiveDataAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the type of masking to apply to this sensitive data.
    /// Defaults to <see cref="SensitiveMaskType.Full"/>.
    /// </summary>
    public SensitiveMaskType MaskType { get; set; } = SensitiveMaskType.Full;

    /// <summary>
    /// Gets or sets an optional description of why this data is considered sensitive.
    /// This helps document security and privacy requirements.
    /// </summary>
    public string? Reason { get; set; }
}
