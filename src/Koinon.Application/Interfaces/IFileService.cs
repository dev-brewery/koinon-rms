using Koinon.Application.DTOs.Files;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for managing file uploads and downloads.
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Uploads a file and creates a BinaryFile record.
    /// </summary>
    /// <param name="request">Upload request with file and metadata</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File metadata DTO</returns>
    Task<FileMetadataDto> UploadFileAsync(UploadFileRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets file metadata by IdKey.
    /// </summary>
    /// <param name="idKey">Encoded ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File metadata or null if not found</returns>
    Task<FileMetadataDto?> GetFileMetadataAsync(string idKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file stream by IdKey.
    /// </summary>
    /// <param name="idKey">Encoded ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of (stream, fileName, mimeType) or null if not found</returns>
    Task<(Stream Stream, string FileName, string MimeType)?> DownloadFileAsync(string idKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file by IdKey.
    /// </summary>
    /// <param name="idKey">Encoded ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteFileAsync(string idKey, CancellationToken cancellationToken = default);
}
