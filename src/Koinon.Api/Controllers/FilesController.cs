using FluentValidation;
using Koinon.Application.DTOs.Files;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for file upload and download operations.
/// </summary>
[ApiController]
[Route("api/v1/files")]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly ILogger<FilesController> _logger;
    private readonly IValidator<UploadFileRequest> _validator;

    public FilesController(
        IFileService fileService,
        ILogger<FilesController> logger,
        IValidator<UploadFileRequest> validator)
    {
        _fileService = fileService;
        _logger = logger;
        _validator = validator;
    }

    /// <summary>
    /// Upload a file.
    /// </summary>
    /// <param name="file">The file to upload</param>
    /// <param name="description">Optional description or alt text</param>
    /// <param name="binaryFileTypeIdKey">Optional file type category IdKey</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File metadata</returns>
    [HttpPost]
    [Authorize]
    [RequestSizeLimit(10_485_760)] // 10MB limit
    [ProducesResponseType(typeof(FileMetadataDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FileMetadataDto>> Upload(
        IFormFile file,
        [FromForm] string? description = null,
        [FromForm] string? binaryFileTypeIdKey = null,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "File is required" });
        }

        try
        {
            // Open stream with proper disposal
            await using var fileStream = file.OpenReadStream();

            // Convert IFormFile to UploadFileRequest DTO (Application layer doesn't know about IFormFile)
            var request = new UploadFileRequest
            {
                Stream = fileStream,
                FileName = file.FileName,
                ContentType = file.ContentType,
                Length = file.Length,
                Description = description,
                BinaryFileTypeIdKey = binaryFileTypeIdKey
            };

            // Validate the request
            await _validator.ValidateAndThrowAsync(request, cancellationToken);

            var result = await _fileService.UploadFileAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetMetadata), new { idKey = result.IdKey }, new { data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file: {FileName}", file.FileName);
            return BadRequest(new { error = "Failed to upload file" });
        }
    }

    /// <summary>
    /// Get file metadata by IdKey.
    /// </summary>
    /// <param name="idKey">Encoded file ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File metadata</returns>
    [HttpGet("{idKey}/metadata")]
    [Authorize]
    [ProducesResponseType(typeof(FileMetadataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileMetadataDto>> GetMetadata(string idKey, CancellationToken cancellationToken)
    {
        var metadata = await _fileService.GetFileMetadataAsync(idKey, cancellationToken);

        if (metadata == null)
        {
            return NotFound();
        }

        return Ok(new { data = metadata });
    }

    /// <summary>
    /// Download a file by IdKey.
    /// </summary>
    /// <param name="idKey">Encoded file ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File stream</returns>
    [HttpGet("{idKey}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(string idKey, CancellationToken cancellationToken)
    {
        var result = await _fileService.DownloadFileAsync(idKey, cancellationToken);

        if (result == null)
        {
            return NotFound();
        }

        var (stream, fileName, mimeType) = result.Value;

        // Return file stream with appropriate headers
        return File(stream, mimeType, fileName, enableRangeProcessing: true);
    }

    /// <summary>
    /// Delete a file by IdKey.
    /// </summary>
    /// <param name="idKey">Encoded file ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{idKey}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string idKey, CancellationToken cancellationToken)
    {
        var deleted = await _fileService.DeleteFileAsync(idKey, cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
