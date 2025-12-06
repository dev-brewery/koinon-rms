using System.Text.RegularExpressions;

namespace Koinon.Api.Helpers;

/// <summary>
/// Validator for IdKey format to prevent malicious input.
/// IdKeys should be URL-safe Base64 encoded strings with specific length constraints.
/// </summary>
public static partial class IdKeyValidator
{
    // URL-safe Base64 pattern: allows A-Z, a-z, 0-9, -, _
    // Length: 6-20 characters (covers typical encoded integers)
    [GeneratedRegex(@"^[A-Za-z0-9_-]{6,20}$", RegexOptions.Compiled)]
    private static partial Regex IdKeyPattern();

    /// <summary>
    /// Validates if a string matches the expected IdKey format.
    /// </summary>
    /// <param name="idKey">The IdKey string to validate.</param>
    /// <returns>True if valid format; otherwise false.</returns>
    public static bool IsValid(string? idKey)
    {
        if (string.IsNullOrWhiteSpace(idKey))
        {
            return false;
        }

        return IdKeyPattern().IsMatch(idKey);
    }

    /// <summary>
    /// Validates multiple IdKey values.
    /// </summary>
    /// <param name="idKeys">The IdKey strings to validate.</param>
    /// <returns>True if all are valid format; otherwise false.</returns>
    public static bool AreAllValid(params string?[] idKeys)
    {
        foreach (var idKey in idKeys)
        {
            if (!IsValid(idKey))
            {
                return false;
            }
        }

        return true;
    }
}
