using FluentValidation.Results;

namespace Koinon.Application.Common;

/// <summary>
/// Represents an error that occurred during an operation.
/// </summary>
/// <param name="Code">Machine-readable error code.</param>
/// <param name="Message">Human-readable error message.</param>
/// <param name="Details">Optional field-level validation errors.</param>
public record Error(string Code, string Message, Dictionary<string, string[]>? Details = null)
{
    /// <summary>
    /// Creates an error from FluentValidation validation failures.
    /// </summary>
    public static Error FromFluentValidation(ValidationResult validationResult)
    {
        var details = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        return new Error(
            "VALIDATION_ERROR",
            "One or more validation errors occurred",
            details
        );
    }

    /// <summary>
    /// Creates a not found error.
    /// </summary>
    public static Error NotFound(string resourceType, string idKey) =>
        new("NOT_FOUND", $"{resourceType} with IdKey '{idKey}' was not found");

    /// <summary>
    /// Creates a conflict error.
    /// </summary>
    public static Error Conflict(string message) =>
        new("CONFLICT", message);

    /// <summary>
    /// Creates an unprocessable entity error (business rule violation).
    /// </summary>
    public static Error UnprocessableEntity(string message) =>
        new("UNPROCESSABLE_ENTITY", message);

    /// <summary>
    /// Creates a not implemented error.
    /// </summary>
    public static Error NotImplemented(string message) =>
        new("NOT_IMPLEMENTED", message);

    /// <summary>
    /// Creates a forbidden error (user lacks permission).
    /// </summary>
    public static Error Forbidden(string message) =>
        new("FORBIDDEN", message);

    /// <summary>
    /// Creates a validation error.
    /// </summary>
    public static Error Validation(string message) =>
        new("VALIDATION_ERROR", message);

    /// <summary>
    /// Creates an internal server error.
    /// </summary>
    public static Error Internal(string message, string? details = null) =>
        new("INTERNAL_ERROR", message, details != null ? new Dictionary<string, string[]> { ["Details"] = new[] { details } } : null);
}
