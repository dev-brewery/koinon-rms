using Koinon.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Koinon.Infrastructure.Services;

/// <summary>
/// Local filesystem-based file storage service for development and simple deployments.
/// For production, consider using S3 or Azure Blob Storage implementations.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storagePath;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IConfiguration configuration, ILogger<LocalFileStorageService> logger)
    {
        _logger = logger;

        // Get storage path from configuration or use default, then normalize to prevent path traversal
        _storagePath = Path.GetFullPath(configuration["FileStorage:LocalPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "file-storage"));

        // Ensure storage directory exists
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
            _logger.LogInformation("Created file storage directory: {StoragePath}", _storagePath);
        }
    }

    public async Task<string> StoreFileAsync(Stream stream, string fileName, string mimeType, CancellationToken cancellationToken = default)
    {
        // Generate unique storage key using GUID to avoid filename conflicts
        var extension = Path.GetExtension(fileName);
        var storageKey = $"{Guid.NewGuid()}{extension}";
        var fullPath = ValidateAndGetFullPath(storageKey);

        try
        {
            await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await stream.CopyToAsync(fileStream, cancellationToken);

            _logger.LogInformation("Stored file {FileName} as {StorageKey}", fileName, storageKey);

            return storageKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store file {FileName}", fileName);
            throw;
        }
    }

    public Task<Stream?> GetFileAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ValidateAndGetFullPath(storageKey);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("File not found: {StorageKey}", storageKey);
            return Task.FromResult<Stream?>(null);
        }

        try
        {
            // Return a new FileStream that the caller must dispose
            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Task.FromResult<Stream?>(fileStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve file {StorageKey}", storageKey);
            throw;
        }
    }

    public Task DeleteFileAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ValidateAndGetFullPath(storageKey);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("Attempted to delete non-existent file: {StorageKey}", storageKey);
            return Task.CompletedTask;
        }

        try
        {
            File.Delete(fullPath);
            _logger.LogInformation("Deleted file: {StorageKey}", storageKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file {StorageKey}", storageKey);
            throw;
        }

        return Task.CompletedTask;
    }

    public Task<bool> FileExistsAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ValidateAndGetFullPath(storageKey);
        return Task.FromResult(File.Exists(fullPath));
    }

    /// <summary>
    /// Validates storage key and returns full path, preventing path traversal attacks.
    /// </summary>
    private string ValidateAndGetFullPath(string storageKey)
    {
        // Combine the paths
        var combinedPath = Path.Combine(_storagePath, storageKey);

        // Get the fully resolved path
        var fullPath = Path.GetFullPath(combinedPath);

        // Ensure the resolved path starts with the storage path (prevents path traversal)
        if (!fullPath.StartsWith(_storagePath, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Path traversal attempt detected: {StorageKey}", storageKey);
            throw new UnauthorizedAccessException($"Invalid storage key: {storageKey}");
        }

        return fullPath;
    }
}
