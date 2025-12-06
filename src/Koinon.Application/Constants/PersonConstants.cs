namespace Koinon.Application.Constants;

/// <summary>
/// Constants for Gender values used in person-related requests and validation.
/// </summary>
public static class GenderValues
{
    /// <summary>
    /// Gender is unknown or not specified.
    /// </summary>
    public const string Unknown = "Unknown";

    /// <summary>
    /// Male gender.
    /// </summary>
    public const string Male = "Male";

    /// <summary>
    /// Female gender.
    /// </summary>
    public const string Female = "Female";

    /// <summary>
    /// Valid gender values for validation.
    /// </summary>
    public static readonly string[] ValidValues = [Unknown, Male, Female];
}

/// <summary>
/// Constants for EmailPreference values used in person-related requests and validation.
/// </summary>
public static class EmailPreferenceValues
{
    /// <summary>
    /// Person has opted in to all email communications.
    /// </summary>
    public const string EmailAllowed = "EmailAllowed";

    /// <summary>
    /// Person wants to receive only essential emails, no mass communications.
    /// </summary>
    public const string NoMassEmails = "NoMassEmails";

    /// <summary>
    /// Person has opted out of all email communications.
    /// </summary>
    public const string DoNotEmail = "DoNotEmail";

    /// <summary>
    /// Valid email preference values for validation.
    /// </summary>
    public static readonly string[] ValidValues = [EmailAllowed, NoMassEmails, DoNotEmail];
}
