using Koinon.Domain.Data;

namespace Koinon.Api.Helpers;

/// <summary>
/// Validates IdKey format for API parameters.
/// IdKeys are URL-safe Base64 encoded integer IDs.
/// </summary>
public static class IdKeyValidator
{
    /// <summary>
    /// Validates that the provided string is a valid IdKey format.
    /// Delegates to IdKeyHelper.TryDecode for consistent validation.
    /// </summary>
    public static bool IsValid(string? idKey)
    {
        if (string.IsNullOrWhiteSpace(idKey))
        {
            return false;
        }

        return IdKeyHelper.TryDecode(idKey, out _);
    }

    /// <summary>
    /// Returns a validation error message for invalid IdKeys.
    /// </summary>
    public static string GetErrorMessage(string parameterName)
        => $"The {parameterName} parameter must be a valid IdKey (URL-safe Base64 encoded identifier)";
}
