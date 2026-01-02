using Koinon.Application.Common;
using Koinon.Application.DTOs;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service interface for communication template management operations.
/// </summary>
public interface ICommunicationTemplateService
{
    /// <summary>
    /// Retrieves a communication template by its IdKey.
    /// </summary>
    /// <param name="idKey">The IdKey of the template.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The template DTO or null if not found.</returns>
    Task<CommunicationTemplateDto?> GetByIdKeyAsync(string idKey, CancellationToken ct = default);

    /// <summary>
    /// Searches for communication templates with optional filters.
    /// </summary>
    /// <param name="type">Optional communication type filter.</param>
    /// <param name="isActive">Optional active status filter.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paged result of template summaries.</returns>
    Task<PagedResult<CommunicationTemplateSummaryDto>> SearchAsync(
        string? type = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a new communication template.
    /// </summary>
    /// <param name="dto">The create template DTO.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created template DTO.</returns>
    Task<Result<CommunicationTemplateDto>> CreateAsync(
        CreateCommunicationTemplateDto dto,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an existing communication template.
    /// </summary>
    /// <param name="idKey">The IdKey of the template to update.</param>
    /// <param name="dto">The update template DTO.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated template DTO.</returns>
    Task<Result<CommunicationTemplateDto>> UpdateAsync(
        string idKey,
        UpdateCommunicationTemplateDto dto,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a communication template.
    /// </summary>
    /// <param name="idKey">The IdKey of the template to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> DeleteAsync(string idKey, CancellationToken ct = default);
}
