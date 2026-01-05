using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service interface for campus management operations.
/// </summary>
public interface ICampusService
{
    /// <summary>
    /// Gets all campuses.
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive campuses.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of campus summaries.</returns>
    Task<IReadOnlyList<CampusSummaryDto>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default);

    /// <summary>
    /// Gets a specific campus by its IdKey.
    /// </summary>
    /// <param name="idKey">The campus's IdKey.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result with campus details or failure.</returns>
    Task<Result<CampusDto>> GetByIdKeyAsync(string idKey, CancellationToken ct = default);

    /// <summary>
    /// Creates a new campus.
    /// </summary>
    /// <param name="request">Campus creation details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result with created campus details or failure.</returns>
    Task<Result<CampusDto>> CreateAsync(CreateCampusRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing campus.
    /// </summary>
    /// <param name="idKey">The campus's IdKey.</param>
    /// <param name="request">Campus update details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result with updated campus details or failure.</returns>
    Task<Result<CampusDto>> UpdateAsync(string idKey, UpdateCampusRequest request, CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes a campus.
    /// </summary>
    /// <param name="idKey">The campus's IdKey.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result representing success or failure.</returns>
    Task<Result> DeleteAsync(string idKey, CancellationToken ct = default);
}
