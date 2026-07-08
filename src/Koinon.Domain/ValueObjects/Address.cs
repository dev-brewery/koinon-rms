namespace Koinon.Domain.ValueObjects;

/// <summary>
/// Value object representing a physical mailing address.
/// Provides formatted output for display purposes.
/// </summary>
public record Address(
    string? Street1,
    string? Street2,
    string? City,
    string? State,
    string? PostalCode,
    string? Country)
{
    /// <summary>
    /// Gets the address formatted for display with line breaks.
    /// </summary>
    public string FormattedAddress => FormatAddress();

    /// <summary>
    /// Formats the address components into a multi-line string.
    /// </summary>
    /// <returns>The formatted address with line breaks between components.</returns>
    private string FormatAddress()
    {
        var parts = new List<string>();

        // Add street lines if present
        if (!string.IsNullOrWhiteSpace(Street1))
        {
            parts.Add(Street1);
        }

        if (!string.IsNullOrWhiteSpace(Street2))
        {
            parts.Add(Street2);
        }

        // Build city, state zip line
        var cityStateZip = string.Join(", ",
            new[] { City, State }.Where(s => !string.IsNullOrWhiteSpace(s)));

        if (!string.IsNullOrWhiteSpace(PostalCode))
        {
            cityStateZip += " " + PostalCode;
        }

        if (!string.IsNullOrWhiteSpace(cityStateZip))
        {
            parts.Add(cityStateZip.Trim());
        }

        // Add country if present
        if (!string.IsNullOrWhiteSpace(Country))
        {
            parts.Add(Country);
        }

        // Explicit LF, not Environment.NewLine: this formatted value can be exported
        // or persisted, so its bytes must not depend on the host OS.
        return string.Join("\n", parts);
    }

    /// <summary>
    /// Creates an empty Address instance.
    /// </summary>
    public static Address Empty => new(null, null, null, null, null, null);
}
