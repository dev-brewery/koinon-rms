namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a file stored in the system (photo, document, etc.).
/// </summary>
public class BinaryFile : Entity
{
    /// <summary>
    /// Original filename when uploaded.
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// MIME type/content type (e.g., "image/jpeg", "application/pdf").
    /// </summary>
    public required string MimeType { get; set; }

    /// <summary>
    /// Path or key in the storage provider (local path, S3 key, etc.).
    /// Never exposed to clients - use IdKey in URLs.
    /// </summary>
    public required string StorageKey { get; set; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Image width in pixels (nullable for non-image files).
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Image height in pixels (nullable for non-image files).
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Foreign key to DefinedValue indicating the file type category (Photo, Document, etc.).
    /// </summary>
    public int? BinaryFileTypeId { get; set; }

    /// <summary>
    /// Optional description or alt text for the file.
    /// </summary>
    public string? Description { get; set; }

    // Navigation properties

    /// <summary>
    /// Navigation property to the file type defined value.
    /// </summary>
    public virtual DefinedValue? BinaryFileType { get; set; }
}
