using Koinon.Application.DTOs;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for generating check-in labels.
/// Performance-critical - label generation should complete in <100ms.
/// </summary>
public interface ILabelGenerationService
{
    /// <summary>
    /// Generates labels for a single check-in.
    /// </summary>
    /// <param name="request">Label generation request with attendance information.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Set of generated labels ready for printing.</returns>
    Task<LabelSetDto> GenerateLabelsAsync(LabelRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Generates labels for multiple check-ins in a batch.
    /// </summary>
    /// <param name="request">Batch label generation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of label sets, one per check-in.</returns>
    Task<IReadOnlyList<LabelSetDto>> GenerateBatchLabelsAsync(BatchLabelRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all available label templates.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of available label templates.</returns>
    Task<IReadOnlyList<LabelTemplateDto>> GetTemplatesAsync(CancellationToken ct = default);

    /// <summary>
    /// Previews a label with sample data.
    /// </summary>
    /// <param name="request">Preview request with sample field values.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>HTML preview of the label.</returns>
    Task<LabelPreviewDto> PreviewLabelAsync(LabelPreviewRequestDto request, CancellationToken ct = default);
}
