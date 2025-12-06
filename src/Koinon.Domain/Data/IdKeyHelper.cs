using System.Buffers;
using System.Buffers.Text;
using System.Text;

namespace Koinon.Domain.Data;

/// <summary>
/// Helper class for encoding and decoding URL-safe Base64 IdKeys.
/// Provides conversion between integer IDs and URL-safe string representations.
/// Uses stack allocation (stackalloc) for high-performance encoding/decoding
/// suitable for handling thousands of ID conversions per second (check-in kiosks).
/// </summary>
public static class IdKeyHelper
{
    /// <summary>
    /// Encodes an integer ID into a URL-safe Base64 string.
    /// </summary>
    /// <param name="id">The integer ID to encode.</param>
    /// <returns>A URL-safe Base64-encoded string.</returns>
    public static string Encode(int id)
    {
        // Convert int to bytes
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BitConverter.TryWriteBytes(bytes, id);

        // Encode to Base64
        Span<byte> base64Bytes = stackalloc byte[Base64.GetMaxEncodedToUtf8Length(sizeof(int))];
        Base64.EncodeToUtf8(bytes, base64Bytes, out _, out int bytesWritten);

        // Convert to URL-safe format: replace + with -, / with _, and remove padding
        var result = Encoding.UTF8.GetString(base64Bytes[..bytesWritten])
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        return result;
    }

    /// <summary>
    /// Decodes a URL-safe Base64 string back to an integer ID.
    /// </summary>
    /// <param name="idKey">The URL-safe Base64-encoded string.</param>
    /// <returns>The decoded integer ID.</returns>
    /// <exception cref="ArgumentException">Thrown when the idKey is null, empty, or invalid.</exception>
    public static int Decode(string idKey)
    {
        if (string.IsNullOrWhiteSpace(idKey))
        {
            throw new ArgumentException("IdKey cannot be null or empty.", nameof(idKey));
        }

        if (!TryDecode(idKey, out int id))
        {
            throw new ArgumentException($"Invalid IdKey format: {idKey}", nameof(idKey));
        }

        return id;
    }

    /// <summary>
    /// Attempts to decode a URL-safe Base64 string back to an integer ID.
    /// </summary>
    /// <param name="idKey">The URL-safe Base64-encoded string.</param>
    /// <param name="id">The decoded integer ID if successful.</param>
    /// <returns>True if the decoding was successful; otherwise, false.</returns>
    public static bool TryDecode(string? idKey, out int id)
    {
        id = 0;

        if (string.IsNullOrWhiteSpace(idKey))
        {
            return false;
        }

        try
        {
            // Convert URL-safe format back to standard Base64
            var base64 = idKey
                .Replace('-', '+')
                .Replace('_', '/');

            // Add padding if needed
            int padding = (4 - (base64.Length % 4)) % 4;
            if (padding > 0)
            {
                base64 += new string('=', padding);
            }

            // Decode from Base64
            Span<byte> base64Bytes = stackalloc byte[base64.Length];
            Encoding.UTF8.GetBytes(base64, base64Bytes);

            Span<byte> decodedBytes = stackalloc byte[sizeof(int)];
            var status = Base64.DecodeFromUtf8(base64Bytes, decodedBytes, out _, out int bytesWritten);

            if (status != OperationStatus.Done)
            {
                return false;
            }

            if (bytesWritten != sizeof(int))
            {
                return false;
            }

            id = BitConverter.ToInt32(decodedBytes);
            return true;
        }
        catch (ArgumentException)
        {
            // Expected: invalid Base64 format or character
            return false;
        }
        catch (FormatException)
        {
            // Expected: invalid encoding format
            return false;
        }
        // Note: Do NOT catch Exception broadly - let unexpected errors propagate
        // for proper debugging and security monitoring
    }
}
