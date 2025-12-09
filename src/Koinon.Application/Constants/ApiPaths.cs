namespace Koinon.Application.Constants;

/// <summary>
/// API path constants for URL generation.
/// </summary>
public static class ApiPaths
{
    /// <summary>
    /// Base API version path.
    /// </summary>
    public const string ApiV1 = "/api/v1";

    /// <summary>
    /// Files endpoint base path.
    /// </summary>
    public const string Files = $"{ApiV1}/files";

    /// <summary>
    /// Generates a file URL from an IdKey.
    /// </summary>
    /// <param name="idKey">The file's IdKey</param>
    /// <returns>Full file URL path</returns>
    public static string GetFileUrl(string idKey) => $"{Files}/{idKey}";
}
