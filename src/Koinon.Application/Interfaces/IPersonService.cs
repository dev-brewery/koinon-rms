using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service interface for person management operations.
/// </summary>
public interface IPersonService
{
    /// <summary>
    /// Gets a person by their integer ID.
    /// </summary>
    Task<PersonDto?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Gets a person by their IdKey.
    /// </summary>
    Task<PersonDto?> GetByIdKeyAsync(string idKey, CancellationToken ct = default);

    /// <summary>
    /// Searches for people with pagination.
    /// </summary>
    Task<PagedResult<PersonSummaryDto>> SearchAsync(
        PersonSearchParameters parameters,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a new person.
    /// </summary>
    Task<Result<PersonDto>> CreateAsync(
        CreatePersonRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an existing person.
    /// </summary>
    Task<Result<PersonDto>> UpdateAsync(
        string idKey,
        UpdatePersonRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes a person (sets record status to inactive).
    /// </summary>
    Task<Result> DeleteAsync(string idKey, CancellationToken ct = default);

    /// <summary>
    /// Gets the person's family with members.
    /// </summary>
    Task<FamilySummaryDto?> GetFamilyAsync(string idKey, CancellationToken ct = default);
}
