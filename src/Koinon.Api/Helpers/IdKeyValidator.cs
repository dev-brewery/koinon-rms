using System.Text.RegularExpressions;

namespace Koinon.Api.Helpers;

/// <summary>
/// Validates IdKey format for API parameters.
/// IdKeys are URL-safe Base64 encoded integer IDs.
/// </summary>
public static partial class IdKeyValidator
{
    // URL-safe Base64 pattern: allows A-Z, a-z, 0-9, -, _
    // Length: 6-20 characters (covers typical encoded integers)
    [GeneratedRegex(@"^[A-Za-z0-9_-]{6,20}$", RegexOptions.Compiled)]
    private static partial Regex IdKeyPattern();

    /// <summary>
    /// Validates that the provided string is a valid IdKey format.
    /// </summary>
    public static bool IsValid(string? idKey)
    {
        if (string.IsNullOrWhiteSpace(idKey))
        {
            return false;
        }

        if (!IdKeyPattern().IsMatch(idKey))
        {
            return false;
        }

        // Attempt to decode as URL-safe Base64
        try
        {
            // Convert URL-safe Base64 to standard Base64
            var standardBase64 = idKey.Replace('-', '+').Replace('_', '/');
            // Add padding if needed
            switch (standardBase64.Length % 4)
            {
                case 2:
                    standardBase64 += "==";
                    break;
                case 3:
                    standardBase64 += "=";
                    break;
            }
            var decoded = Convert.FromBase64String(standardBase64);
            return decoded.Length >= 1; // Must decode to at least 1 byte
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Returns a validation error message for invalid IdKeys.
    /// </summary>
    public static string GetErrorMessage(string parameterName)
        => $"The {parameterName} parameter must be a valid IdKey (URL-safe Base64 encoded identifier)";
}
