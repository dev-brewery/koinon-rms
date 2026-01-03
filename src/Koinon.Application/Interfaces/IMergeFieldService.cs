using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Domain.Entities;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for managing merge fields in communication templates.
/// Merge fields are tokens like {{FirstName}} that get replaced with person-specific data.
/// </summary>
public interface IMergeFieldService
{
    /// <summary>
    /// Gets all available merge fields that can be used in templates.
    /// </summary>
    /// <returns>Read-only list of supported merge field definitions.</returns>
    IReadOnlyList<MergeFieldDto> GetAvailableMergeFields();

    /// <summary>
    /// Replaces all merge field tokens in the template with values from the person.
    /// </summary>
    /// <param name="template">The text template containing merge field tokens (e.g., "Hello {{FirstName}}").</param>
    /// <param name="person">The person whose data will be used for replacement.</param>
    /// <returns>The template with all merge fields replaced with person data. Unknown tokens are left unchanged.</returns>
    /// <remarks>
    /// - {{FirstName}} → person.FirstName
    /// - {{LastName}} → person.LastName
    /// - {{NickName}} → person.NickName ?? person.FirstName (fallback)
    /// - {{FullName}} → person.FirstName + " " + person.LastName
    /// - {{Email}} → person.Email
    /// 
    /// Null or empty values are replaced with empty string.
    /// </remarks>
    string ReplaceMergeFields(string template, Person person);

    /// <summary>
    /// Validates that all merge field tokens in the text are recognized.
    /// </summary>
    /// <param name="text">The text to validate for merge field usage.</param>
    /// <returns>
    /// Success if all tokens are valid.
    /// Failure with error details if unknown merge fields are detected.
    /// </returns>
    Result ValidateMergeFields(string text);
}
