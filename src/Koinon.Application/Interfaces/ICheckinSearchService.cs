using Koinon.Application.DTOs;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service interface for check-in family search operations.
/// Optimized for fast lookup during Sunday morning check-in kiosk operations.
/// </summary>
public interface ICheckinSearchService
{
    /// <summary>
    /// Searches for families by phone number (last 4 digits or full number).
    /// </summary>
    /// <param name="phoneNumber">Phone number (full or last 4 digits)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of matching families with members</returns>
    Task<List<CheckinFamilySearchResultDto>> SearchByPhoneAsync(
        string phoneNumber,
        CancellationToken ct = default);

    /// <summary>
    /// Searches for families by name (partial match on first/last/nick name).
    /// </summary>
    /// <param name="name">Name to search for (case-insensitive partial match)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of matching families with members</returns>
    Task<List<CheckinFamilySearchResultDto>> SearchByNameAsync(
        string name,
        CancellationToken ct = default);

    /// <summary>
    /// Searches for a family by check-in security code issued today.
    /// </summary>
    /// <param name="code">Security code from check-in label</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Matching family if found</returns>
    Task<CheckinFamilySearchResultDto?> SearchByCodeAsync(
        string code,
        CancellationToken ct = default);

    /// <summary>
    /// Searches for families using a combined query (phone, name, or code).
    /// Automatically detects query type and routes to appropriate search method.
    /// </summary>
    /// <param name="query">Search query (phone, name, or code)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of matching families with members</returns>
    Task<List<CheckinFamilySearchResultDto>> SearchAsync(
        string query,
        CancellationToken ct = default);
}
