using Koinon.Application.DTOs;

namespace Koinon.Application.DTOs.Files;

/// <summary>
/// File metadata response DTO.
/// </summary>
public class FileMetadataDto
{
    /// <summary>
    /// Encoded ID for use in URLs.
    /// </summary>
    public required string IdKey { get; set; }

    /// <summary>
    /// Original filename.
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// MIME type/content type.
    /// </summary>
    public required string MimeType { get; set; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Image width in pixels (null for non-images).
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Image height in pixels (null for non-images).
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// File type category (DefinedValue).
    /// </summary>
    public DefinedValueDto? BinaryFileType { get; set; }

    /// <summary>
    /// Optional description or alt text.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// When the file was uploaded.
    /// </summary>
    public DateTime CreatedDateTime { get; set; }

    /// <summary>
    /// URL to download/view the file.
    /// </summary>
    public required string Url { get; set; }
}
