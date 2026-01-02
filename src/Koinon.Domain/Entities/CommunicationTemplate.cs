using Koinon.Domain.Enums;

namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a reusable template for communications (email or SMS).
/// Templates can be used to standardize messaging across the system.
/// </summary>
public class CommunicationTemplate : Entity
{
    /// <summary>
    /// The display name of the template.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The type of communication this template is for (Email or SMS).
    /// </summary>
    public required CommunicationType CommunicationType { get; set; }

    /// <summary>
    /// The subject line for email communications.
    /// Not used for SMS templates.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// The message body content.
    /// Can contain merge fields/placeholders for personalization.
    /// </summary>
    public required string Body { get; set; }

    /// <summary>
    /// Internal description or notes about this template.
    /// Used for administrative purposes.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Indicates whether this template is available for use.
    /// Inactive templates are not shown in template selection lists.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
