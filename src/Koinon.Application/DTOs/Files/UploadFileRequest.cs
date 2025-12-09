namespace Koinon.Application.DTOs.Files;

/// <summary>
/// Request DTO for file upload.
/// Application layer uses Stream-based file representation to avoid AspNetCore dependencies.
/// The API layer converts IFormFile to this DTO.
/// </summary>
public class UploadFileRequest
{
    /// <summary>
    /// File content stream.
    /// </summary>
    public required Stream Stream { get; set; }

    /// <summary>
    /// Original filename.
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// MIME type/content type.
    /// </summary>
    public required string ContentType { get; set; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long Length { get; set; }

    /// <summary>
    /// Optional description or alt text for the file.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional file type category IdKey (from DefinedValue).
    /// </summary>
    public string? BinaryFileTypeIdKey { get; set; }
}
