using Koinon.Application.Common;
using Koinon.Application.DTOs;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service interface for communication management operations.
/// </summary>
public interface ICommunicationService
{
    /// <summary>
    /// Gets a communication by its IdKey with all recipients.
    /// </summary>
    Task<CommunicationDto?> GetByIdKeyAsync(string idKey, CancellationToken ct = default);

    /// <summary>
    /// Searches for communications with pagination.
    /// </summary>
    Task<PagedResult<CommunicationSummaryDto>> SearchAsync(
        int page = 1,
        int pageSize = 20,
        string? status = null,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a new communication.
    /// </summary>
    Task<Result<CommunicationDto>> CreateAsync(
        CreateCommunicationDto dto,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an existing communication (only allowed if status is Draft).
    /// </summary>
    Task<Result<CommunicationDto>> UpdateAsync(
        string idKey,
        UpdateCommunicationDto dto,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a communication (only allowed if status is Draft).
    /// </summary>
    Task<Result> DeleteAsync(string idKey, CancellationToken ct = default);

    /// <summary>
    /// Queues a communication for sending (changes status from Draft to Pending).
    /// </summary>
    Task<Result<CommunicationDto>> SendAsync(string idKey, CancellationToken ct = default);
}
