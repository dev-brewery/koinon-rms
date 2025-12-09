using System.Text.RegularExpressions;

namespace Koinon.Application.Helpers;

/// <summary>
/// Helper for phone number normalization and validation.
/// Converts common US phone formats to E.164 format.
/// </summary>
public static class PhoneNumberHelper
{
    private static readonly Regex E164Regex = new(@"^\+[1-9]\d{1,14}$", RegexOptions.Compiled);

    /// <summary>
    /// Normalizes phone number to E.164 format.
    /// Accepts formats like (555) 123-4567, 555-123-4567, 555.123.4567
    /// </summary>
    /// <param name="input">Raw phone number input</param>
    /// <returns>Normalized E.164 format phone number, or null if input is invalid</returns>
    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        // Keep leading + if present
        var hasPlus = input.TrimStart().StartsWith('+');

        // Strip all non-digits
        var digits = new string(input.Where(char.IsDigit).ToArray());

        if (string.IsNullOrEmpty(digits))
        {
            return null;
        }

        // Reject numbers starting with 0 (invalid for E.164)
        if (digits.StartsWith('0'))
        {
            return null;
        }

        // E.164 allows 1-15 digits total
        // If less than 7 digits (minimum for most countries), reject
        if (digits.Length < 7)
        {
            return null;
        }

        // If more than 15 digits, reject
        if (digits.Length > 15)
        {
            return null;
        }

        // If exactly 10 digits and no country code, assume US (+1)
        if (digits.Length == 10 && !hasPlus)
        {
            return $"+1{digits}";
        }

        // If 11 digits starting with 1, add +
        if (digits.Length == 11 && digits.StartsWith('1') && !hasPlus)
        {
            return $"+{digits}";
        }

        // Otherwise, add + prefix
        return $"+{digits}";
    }

    /// <summary>
    /// Validates that a phone number is in E.164 format.
    /// </summary>
    /// <param name="phone">Phone number to validate</param>
    /// <returns>True if valid E.164 format, false otherwise</returns>
    public static bool IsValidE164(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return false;
        }

        return E164Regex.IsMatch(phone);
    }

    /// <summary>
    /// Normalizes and validates a phone number.
    /// Returns the normalized E.164 format if valid, null otherwise.
    /// </summary>
    /// <param name="input">Raw phone number input</param>
    /// <returns>Normalized E.164 format if valid, null otherwise</returns>
    public static string? NormalizeAndValidate(string? input)
    {
        var normalized = Normalize(input);
        if (normalized == null || !IsValidE164(normalized))
        {
            return null;
        }

        return normalized;
    }
}
