using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Domain.Enums;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service interface for managing communication preferences.
/// </summary>
public interface ICommunicationPreferenceService
{
    /// <summary>
    /// Gets all communication preferences for a person.
    /// Returns all communication types, using defaults for types without explicit preferences.
    /// </summary>
    /// <param name="personIdKey">The person's IdKey.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of communication preferences for all types.</returns>
    Task<List<CommunicationPreferenceDto>> GetByPersonAsync(string personIdKey, CancellationToken ct = default);

    /// <summary>
    /// Updates a person's preference for a specific communication type.
    /// Creates a new preference record if one doesn't exist.
    /// </summary>
    /// <param name="personIdKey">The person's IdKey.</param>
    /// <param name="communicationType">The communication type to update (Email or Sms).</param>
    /// <param name="dto">The preference update data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated preference.</returns>
    Task<Result<CommunicationPreferenceDto>> UpdateAsync(
        string personIdKey,
        string communicationType,
        UpdateCommunicationPreferenceDto dto,
        CancellationToken ct = default);

    /// <summary>
    /// Updates multiple communication preferences for a person in a single operation.
    /// </summary>
    /// <param name="personIdKey">The person's IdKey.</param>
    /// <param name="dto">The bulk update data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of updated preferences.</returns>
    Task<Result<List<CommunicationPreferenceDto>>> BulkUpdateAsync(
        string personIdKey,
        BulkUpdatePreferencesDto dto,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if a person has opted out of a specific communication type.
    /// Returns false (opted in) if no preference record exists.
    /// </summary>
    /// <param name="personId">The person's database ID.</param>
    /// <param name="type">The communication type to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if opted out, false if opted in.</returns>
    Task<bool> IsOptedOutAsync(int personId, CommunicationType type, CancellationToken ct = default);

    /// <summary>
    /// Checks if multiple persons have opted out of a specific communication type.
    /// Returns false (opted in) if no preference record exists for a person.
    /// </summary>
    /// <param name="personIds">List of person database IDs to check.</param>
    /// <param name="type">The communication type to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Dictionary mapping PersonId to opt-out status (true = opted out, false = opted in).</returns>
    Task<Dictionary<int, bool>> IsOptedOutBatchAsync(List<int> personIds, CommunicationType type, CancellationToken ct = default);
}
