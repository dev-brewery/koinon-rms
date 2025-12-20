using Koinon.Application.Common;
using Koinon.Application.DTOs.Giving;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for manual contribution entry with batch reconciliation.
/// </summary>
public interface IBatchDonationEntryService
{
    /// <summary>
    /// Creates a new contribution batch.
    /// </summary>
    /// <param name="request">The batch creation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created batch.</returns>
    Task<Result<ContributionBatchDto>> CreateBatchAsync(CreateBatchRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets a batch by its IdKey.
    /// </summary>
    /// <param name="batchIdKey">The batch IdKey.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The batch if found.</returns>
    Task<Result<ContributionBatchDto>> GetBatchAsync(string batchIdKey, CancellationToken ct = default);

    /// <summary>
    /// Gets a batch summary with reconciliation status.
    /// </summary>
    /// <param name="batchIdKey">The batch IdKey.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The batch summary with variance calculation.</returns>
    Task<Result<BatchSummaryDto>> GetBatchSummaryAsync(string batchIdKey, CancellationToken ct = default);

    /// <summary>
    /// Opens a batch for editing (no-op if already open, error if closed/posted).
    /// </summary>
    /// <param name="batchIdKey">The batch IdKey.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> OpenBatchAsync(string batchIdKey, CancellationToken ct = default);

    /// <summary>
    /// Closes a batch (can only close open batches).
    /// </summary>
    /// <param name="batchIdKey">The batch IdKey.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> CloseBatchAsync(string batchIdKey, CancellationToken ct = default);

    /// <summary>
    /// Adds a contribution to a batch (only allowed for open batches).
    /// </summary>
    /// <param name="batchIdKey">The batch IdKey.</param>
    /// <param name="request">The contribution request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created contribution.</returns>
    Task<Result<ContributionDto>> AddContributionAsync(string batchIdKey, AddContributionRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing contribution (only allowed if parent batch is open).
    /// </summary>
    /// <param name="contributionIdKey">The contribution IdKey.</param>
    /// <param name="request">The update request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated contribution.</returns>
    Task<Result<ContributionDto>> UpdateContributionAsync(string contributionIdKey, UpdateContributionRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes a contribution (only allowed if parent batch is open).
    /// </summary>
    /// <param name="contributionIdKey">The contribution IdKey.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> DeleteContributionAsync(string contributionIdKey, CancellationToken ct = default);

    /// <summary>
    /// Searches for contributors by name or email.
    /// </summary>
    /// <param name="searchTerm">The search term.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Matching people for lookup.</returns>
    Task<IReadOnlyList<PersonLookupDto>> SearchContributorsAsync(string searchTerm, CancellationToken ct = default);

    /// <summary>
    /// Gets all active funds for contribution entry.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of active funds.</returns>
    Task<IReadOnlyList<FundDto>> GetActiveFundsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a fund by its IdKey.
    /// </summary>
    /// <param name="idKey">The fund IdKey.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The fund if found.</returns>
    Task<Result<FundDto>> GetFundAsync(string idKey, CancellationToken ct = default);

    /// <summary>
    /// Gets a paginated list of batches with optional filters.
    /// </summary>
    /// <param name="filter">Filter and pagination parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated list of batches.</returns>
    Task<PagedResult<ContributionBatchDto>> GetBatchesAsync(BatchFilterRequest filter, CancellationToken ct = default);

    /// <summary>
    /// Gets all contributions in a batch.
    /// </summary>
    /// <param name="batchIdKey">The batch IdKey.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of contributions in the batch.</returns>
    Task<Result<IReadOnlyList<ContributionDto>>> GetBatchContributionsAsync(string batchIdKey, CancellationToken ct = default);

    /// <summary>
    /// Gets a contribution by its IdKey.
    /// </summary>
    /// <param name="idKey">The contribution IdKey.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The contribution if found.</returns>
    Task<Result<ContributionDto>> GetContributionAsync(string idKey, CancellationToken ct = default);
}
