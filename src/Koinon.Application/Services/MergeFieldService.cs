using System.Text.RegularExpressions;
using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Koinon.Domain.Entities;

namespace Koinon.Application.Services;

/// <summary>
/// Service for managing merge fields in communication templates.
/// Handles field definition, validation, and replacement with person data.
/// </summary>
public partial class MergeFieldService : IMergeFieldService
{
    // Regex pattern to match merge field tokens: {{FieldName}}
    // Pattern: {{ followed by word characters, followed by }}
    [GeneratedRegex(@"\{\{(\w+)\}\}", RegexOptions.Compiled)]
    private static partial Regex MergeFieldTokenRegex();

    // Static list of supported merge fields
    private static readonly IReadOnlyList<MergeFieldDto> _availableFields = new List<MergeFieldDto>
    {
        new("FirstName", "{{FirstName}}", "Recipient's first name"),
        new("LastName", "{{LastName}}", "Recipient's last name"),
        new("NickName", "{{NickName}}", "Recipient's nickname (falls back to first name if not set)"),
        new("FullName", "{{FullName}}", "Recipient's full name (first name + last name)"),
        new("Email", "{{Email}}", "Recipient's email address")
    }.AsReadOnly();

    public IReadOnlyList<MergeFieldDto> GetAvailableMergeFields()
    {
        return _availableFields;
    }

    public string ReplaceMergeFields(string template, Person person)
    {
        if (string.IsNullOrEmpty(template))
        {
            return template;
        }

        // Use regex to find and replace each merge field token
        var result = MergeFieldTokenRegex().Replace(template, match =>
        {
            var fieldName = match.Groups[1].Value;
            return GetFieldValue(fieldName, person);
        });

        return result;
    }

    public Result ValidateMergeFields(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Result.Success();
        }

        var matches = MergeFieldTokenRegex().Matches(text);
        var invalidFields = new List<string>();

        foreach (Match match in matches)
        {
            var fieldName = match.Groups[1].Value;
            if (!IsValidFieldName(fieldName))
            {
                invalidFields.Add(match.Value); // Include the full token {{FieldName}}
            }
        }

        if (invalidFields.Count > 0)
        {
            var invalidFieldList = string.Join(", ", invalidFields.Distinct());
            return Result.Failure(new Error(
                "INVALID_MERGE_FIELDS",
                $"Unknown merge field(s): {invalidFieldList}. Valid fields are: {string.Join(", ", _availableFields.Select(f => f.Token))}"
            ));
        }

        return Result.Success();
    }

    /// <summary>
    /// Gets the value for a specific merge field from the person entity.
    /// </summary>
    /// <param name="fieldName">The field name (without delimiters).</param>
    /// <param name="person">The person entity to extract data from.</param>
    /// <returns>The field value, or the original token if field is not recognized.</returns>
    private static string GetFieldValue(string fieldName, Person person)
    {
        return fieldName switch
        {
            "FirstName" => person.FirstName ?? string.Empty,
            "LastName" => person.LastName ?? string.Empty,
            "NickName" => !string.IsNullOrWhiteSpace(person.NickName) ? person.NickName : person.FirstName ?? string.Empty,
            "FullName" => $"{person.FirstName} {person.LastName}".Trim(),
            "Email" => person.Email ?? string.Empty,
            _ => $"{{{{{fieldName}}}}}" // Return the original token if not recognized
        };
    }

    /// <summary>
    /// Checks if a field name is valid (exists in the supported fields list).
    /// </summary>
    /// <param name="fieldName">The field name to validate.</param>
    /// <returns>True if the field is supported, false otherwise.</returns>
    private static bool IsValidFieldName(string fieldName)
    {
        return _availableFields.Any(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
    }
}
