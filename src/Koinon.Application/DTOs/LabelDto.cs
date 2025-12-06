using Koinon.Domain.Enums;

namespace Koinon.Application.DTOs;

/// <summary>
/// Represents a set of labels generated for a single check-in.
/// </summary>
public record LabelSetDto(
    string AttendanceIdKey,
    string PersonIdKey,
    IReadOnlyList<LabelDto> Labels
);

/// <summary>
/// Represents a single generated label with content and metadata.
/// </summary>
public record LabelDto(
    LabelType Type,
    string Content,
    string Format,
    IDictionary<string, string> Fields
);

/// <summary>
/// Represents a label template configuration.
/// </summary>
public record LabelTemplateDto(
    string IdKey,
    string Name,
    LabelType Type,
    string Format,
    string Template,
    int WidthMm,
    int HeightMm
);

/// <summary>
/// Request for generating labels for a check-in.
/// </summary>
public record LabelRequestDto
{
    /// <summary>
    /// The attendance record ID for which to generate labels.
    /// </summary>
    public required string AttendanceIdKey { get; init; }

    /// <summary>
    /// Optional list of specific label types to generate.
    /// If null or empty, generates default labels based on person age and configuration.
    /// </summary>
    public IReadOnlyList<LabelType>? LabelTypes { get; init; }

    /// <summary>
    /// Optional custom fields to include in label generation.
    /// </summary>
    public IDictionary<string, string>? CustomFields { get; init; }
}

/// <summary>
/// Request for generating labels for multiple check-ins in a batch.
/// </summary>
public record BatchLabelRequestDto
{
    /// <summary>
    /// Collection of attendance IDs to generate labels for.
    /// </summary>
    public required IReadOnlyList<string> AttendanceIdKeys { get; init; }

    /// <summary>
    /// Optional list of specific label types to generate.
    /// If null or empty, generates default labels based on person age and configuration.
    /// </summary>
    public IReadOnlyList<LabelType>? LabelTypes { get; init; }

    /// <summary>
    /// Optional custom fields to include in label generation.
    /// </summary>
    public IDictionary<string, string>? CustomFields { get; init; }
}

/// <summary>
/// Request for previewing a label.
/// </summary>
public record LabelPreviewRequestDto
{
    /// <summary>
    /// The type of label to preview.
    /// </summary>
    public required LabelType Type { get; init; }

    /// <summary>
    /// Sample field values for preview.
    /// </summary>
    public required IDictionary<string, string> Fields { get; init; }

    /// <summary>
    /// Optional template ID to use. If null, uses default template for label type.
    /// </summary>
    public string? TemplateIdKey { get; init; }
}

/// <summary>
/// Preview of a label (HTML or image representation).
/// </summary>
public record LabelPreviewDto(
    LabelType Type,
    string PreviewHtml,
    string Format
);
