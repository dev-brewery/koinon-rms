namespace Koinon.Application.Interfaces;

/// <summary>
/// Interface for file storage operations (local, S3, Azure Blob, etc.).
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Stores a file and returns a storage key (path, S3 key, etc.).
    /// </summary>
    /// <param name="stream">File content stream</param>
    /// <param name="fileName">Original filename</param>
    /// <param name="mimeType">File MIME type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Storage key that can be used to retrieve the file</returns>
    Task<string> StoreFileAsync(Stream stream, string fileName, string mimeType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a file stream by storage key.
    /// </summary>
    /// <param name="storageKey">Storage key (from StoreFileAsync)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File stream or null if not found</returns>
    Task<Stream?> GetFileAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file by storage key.
    /// </summary>
    /// <param name="storageKey">Storage key (from StoreFileAsync)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteFileAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists.
    /// </summary>
    /// <param name="storageKey">Storage key (from StoreFileAsync)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if file exists</returns>
    Task<bool> FileExistsAsync(string storageKey, CancellationToken cancellationToken = default);
}
