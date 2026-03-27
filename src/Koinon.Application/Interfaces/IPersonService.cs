using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Giving;
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
    /// Returns Success(null) when person exists but has no family.
    /// Returns Failure(NotFound) when person doesn't exist.
    /// </summary>
    Task<Result<FamilySummaryDto?>> GetFamilyAsync(string idKey, CancellationToken ct = default);

    /// <summary>
    /// Updates a person's photo.
    /// </summary>
    /// <param name="idKey">Person's IdKey</param>
    /// <param name="photoIdKey">Photo's IdKey (BinaryFile)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result with updated person DTO</returns>
    Task<Result<PersonDto>> UpdatePhotoAsync(string idKey, string? photoIdKey, CancellationToken ct = default);

    /// <summary>
    /// Gets the groups a person belongs to (excluding family groups).
    /// </summary>
    /// <param name="idKey">Person's IdKey</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of group memberships</returns>
    Task<PagedResult<PersonGroupMembershipDto>> GetGroupsAsync(
        string idKey,
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default);

    /// <summary>
    /// Gets paginated notes for a person, ordered by NoteDateTime descending.
    /// Returns an empty page when the person IdKey is invalid.
    /// </summary>
    Task<Result<PagedResult<NoteDto>>> GetNotesAsync(
        string personIdKey,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a new note on a person's record.
    /// </summary>
    Task<Result<NoteDto>> CreateNoteAsync(
        string personIdKey,
        CreateNoteRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an existing note by its IdKey.
    /// </summary>
    Task<Result<NoteDto>> UpdateNoteAsync(
        string noteIdKey,
        UpdateNoteRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Hard-deletes a note by its IdKey.
    /// </summary>
    Task<Result> DeleteNoteAsync(string noteIdKey, CancellationToken ct = default);

    /// <summary>
    /// Gets a person's giving summary: YTD total, last contribution date, and last 10 contributions.
    /// Returns Failure(NotFound) when the person does not exist.
    /// </summary>
    /// <param name="idKey">Person's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result containing the giving summary DTO</returns>
    Task<Result<PersonGivingSummaryDto>> GetGivingSummaryAsync(string idKey, CancellationToken ct = default);
}
