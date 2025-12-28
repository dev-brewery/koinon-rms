namespace Koinon.Domain.Entities;

/// <summary>
/// Entity with Unicode characters in strings and comments.
/// Tests handling of non-ASCII characters: 日本語, العربية, 中文, Ελληνικά
/// </summary>
public class UnicodeTestEntity : Entity
{
    /// <summary>
    /// Name with emoji support 🎉🎊✨
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Description avec des caractères français: àéèêëïôù
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// German characters: ÄäÖöÜüß
    /// </summary>
    public string? GermanText { get; set; }

    /// <summary>
    /// Spanish characters: ñÑáéíóúü¿¡
    /// </summary>
    public string? SpanishText { get; set; }

    /// <summary>
    /// Math symbols: ∑∫∂√∞≈≠±×÷
    /// </summary>
    public string? MathSymbols { get; set; }

    /// <summary>
    /// Currency symbols: $€£¥₹₽₩
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// Zero-width characters and combining marks: café vs café (different Unicode)
    /// </summary>
    public string? NormalizedText { get; set; }
}
